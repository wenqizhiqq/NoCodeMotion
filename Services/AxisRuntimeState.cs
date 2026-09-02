// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
using System;
using System.Collections.Generic;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 轴实时位置运行态（单例）。
    /// 由硬件对接桩在每次下发轴运动时写入，作为 3D 仿真"根据流程自动生成"的数据源：
    /// 流程/单步运行时轴位置变化 → 这里更新 → Sim3DView 每帧读取驱动机台各轴组与当前位置头。
    /// 不依赖具体轴数量/类型，按轴名索引，天然支持任意轴配置的参数化机台。
    /// </summary>
    public static class AxisRuntimeState
    {
        private static readonly Dictionary<string, double> _pos = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new();
        private static bool _hasAny;

        public static void Set(string name, double value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            lock (_lock) { _pos[name] = value; _hasAny = true; }
        }

        public static double Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            lock (_lock) return _pos.TryGetValue(name, out var v) ? v : 0;
        }

        public static bool HasAny
        {
            get { lock (_lock) return _hasAny; }
        }

        public static void Clear()
        {
            lock (_lock) { _pos.Clear(); _hasAny = false; }
        }
    }
}
