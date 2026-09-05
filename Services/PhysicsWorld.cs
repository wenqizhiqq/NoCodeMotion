// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// 仿真物理世界（封装 BepuPhysics v2，纯 C#、无原生 DLL）。
// 提供：工件动态刚体（重力 + 落于床面）、床面静态碰撞体、气缸杆/主轴运动学碰撞体的增删与步进，
// 以及工件拾取/拖拽所需的位姿读写。机台其余部件保持运动学（由流程运行时驱动），
// 其运动学碰撞体每帧跟随流程位置移动，从而物理推动工件——实现「仿真与流程交互」。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;

namespace NoCodeMotion.Services
{
    /// <summary>轻封装 BepuPhysics v2：仿真工件物理化（重力 / 碰撞 / 被气缸与轴推动）。</summary>
    public sealed class PhysicsWorld : IDisposable
    {
        private BufferPool _pool;
        private Simulation _sim;

        // 工件（动态盒）
        private BodyHandle _wpHandle;
        private BodyInertia _wpInertia;
        private float _wpMass;
        private bool _wpReady;

        // 运动学碰撞体（气缸杆 / 主轴）
        private readonly Dictionary<string, BodyHandle> _kin = new(StringComparer.OrdinalIgnoreCase);

        private bool _disposed;

        // 窄相位回调：允许含动态体的接触，定义接触材质（摩擦/回弹）。
        private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
        {
            public void Initialize(Simulation simulation) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
                => a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
                where TManifold : unmanaged, IContactManifold<TManifold>
            {
                pairMaterial = new PairMaterialProperties();
                pairMaterial.FrictionCoefficient = 0.8f;
                pairMaterial.MaximumRecoveryVelocity = 2f;
                pairMaterial.SpringSettings = new SpringSettings(30f, 1f);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;

            public void Dispose() { }
        }

        // 位姿积分回调：单向重力 + 轻微阻尼。
        private struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
        {
            public Vector3 Gravity;
            public float LinearDamping;
            public float AngularDamping;

            private Vector3Wide _gravityDt;
            private Vector<float> _linearDampingDt;
            private Vector<float> _angularDampingDt;

            public PoseIntegratorCallbacks(Vector3 gravity, float linearDamping = 0f, float angularDamping = 0f) : this()
            {
                Gravity = gravity;
                LinearDamping = linearDamping;
                AngularDamping = angularDamping;
            }

            public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
            public readonly bool AllowSubstepsForUnconstrainedBodies => false;
            public readonly bool IntegrateVelocityForKinematics => false;

            public void Initialize(Simulation simulation) { }

            public void PrepareForIntegration(float dt)
            {
                _gravityDt = Vector3Wide.Broadcast(Gravity * dt);
                _linearDampingDt = new Vector<float>(MathF.Pow(Math.Max(0f, 1f - LinearDamping), dt));
                _angularDampingDt = new Vector<float>(MathF.Pow(Math.Max(0f, 1f - AngularDamping), dt));
            }

            public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
            {
                velocity.Linear += _gravityDt;
                velocity.Linear *= _linearDampingDt;
                velocity.Angular *= _angularDampingDt;
            }
        }

        public PhysicsWorld(Vector3 gravity)
        {
            _pool = new BufferPool();
            _sim = Simulation.Create(_pool, new NarrowPhaseCallbacks(), new PoseIntegratorCallbacks(gravity), new SolveDescription(1, 1));
        }

        /// <summary>床面静态碰撞体（盒，全尺寸）。top = centerY + halfY。</summary>
        public void AddFloor(Vector3 center, Vector3 fullSize)
        {
            var shape = _sim.Shapes.Add(new Box(fullSize.X, fullSize.Y, fullSize.Z));
            var handle = _sim.Statics.Add(new StaticDescription(new RigidPose(center, Quaternion.Identity), shape));
        }

        /// <summary>添加工件动态盒（全尺寸 + 质量），返回刚体句柄。</summary>
        public BodyHandle AddWorkpiece(Vector3 center, Vector3 fullSize, float mass)
        {
            var box = new Box(fullSize.X, fullSize.Y, fullSize.Z);
            var shape = _sim.Shapes.Add(box);
            _wpInertia = box.ComputeInertia(mass);
            _wpMass = mass;
            var body = BodyDescription.CreateDynamic(
                new RigidPose(center, Quaternion.Identity),
                _wpInertia,
                new CollidableDescription(shape, 0.1f),
                new BodyActivityDescription(0.01f));
            _wpHandle = _sim.Bodies.Add(body);
            _wpReady = true;
            return _wpHandle;
        }

        /// <summary>添加运动学盒碰撞体（全尺寸），返回句柄；其位姿每帧由流程运行时驱动。</summary>
        public BodyHandle AddKinematic(Vector3 center, Vector3 fullSize)
        {
            var shape = _sim.Shapes.Add(new Box(fullSize.X, fullSize.Y, fullSize.Z));
            var body = BodyDescription.CreateKinematic(
                center,
                new CollidableDescription(shape, 0.1f),
                new BodyActivityDescription(-1f));
            return _sim.Bodies.Add(body);
        }

        public void RegisterKinematic(string key, BodyHandle handle) => _kin[key] = handle;

        /// <summary>设置运动学碰撞体目标位姿（世界坐标）。以速度驱动：让积分器按 (目标-当前)/dt 移动，
        /// 接触求解才能产生推力，从而物理推动工件（实现「与流程交互」）。</summary>
        public void SetKinematicPose(BodyHandle handle, Vector3 pos, Quaternion? rot = null, float dt = 1f / 60f)
        {
            if (handle.Value < 0) return;
            var br = _sim.Bodies.GetBodyReference(handle);
            br.Velocity.Linear = (pos - br.Pose.Position) / dt;
            if (rot != null)
                br.Pose = new RigidPose(br.Pose.Position, rot.Value);
        }

        public void SetKinematicPose(string key, Vector3 pos, Quaternion? rot = null)
        {
            if (_kin.TryGetValue(key, out var h)) SetKinematicPose(h, pos, rot);
        }

        /// <summary>把工件切换为运动学（拖拽时调用），停受重力；恢复时回到动态。</summary>
        public void SetWorkpieceKinematic(bool kinematic)
        {
            if (!_wpReady) return;
            var br = _sim.Bodies.GetBodyReference(_wpHandle);
            if (kinematic) br.BecomeKinematic();
            else br.SetLocalInertia(_wpInertia);
        }

        /// <summary>直接设置工件位姿（拖拽中每帧调用）。</summary>
        public void SetWorkpiecePose(Vector3 pos, Quaternion? rot = null)
        {
            if (!_wpReady) return;
            var br = _sim.Bodies.GetBodyReference(_wpHandle);
            br.Pose = new RigidPose(pos, rot ?? br.Pose.Orientation);
        }

        /// <summary>读取工件当前位姿（世界坐标）。</summary>
        public void GetWorkpiecePose(out Vector3 pos, out Quaternion rot)
        {
            if (_wpReady)
            {
                var br = _sim.Bodies.GetBodyReference(_wpHandle);
                pos = br.Pose.Position;
                rot = br.Pose.Orientation;
            }
            else
            {
                pos = Vector3.Zero;
                rot = Quaternion.Identity;
            }
        }

        /// <summary>推进一个固定步长，返回工件最新位姿供可视化。</summary>
        public void Step(float dt, out Vector3 wpPos, out Quaternion wpRot)
        {
            _sim.Timestep(dt);
            GetWorkpiecePose(out wpPos, out wpRot);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _sim?.Dispose();
            _pool?.Clear();
        }
    }
}
