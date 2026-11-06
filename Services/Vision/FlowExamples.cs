// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>一个现成的流程例子：内部键 + 白话名 + 一句话说明 + 造步骤。</summary>
    public sealed class FlowExample
    {
        /// <summary>内部键（界面按它找例子，改名不动键）。</summary>
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary>这一串步骤在干什么、参数是按什么填的（界面上直接显示）。</summary>
        public string Help { get; init; } = "";
        public Func<VisualFlowStep[]> Build { get; init; } = () => Array.Empty<VisualFlowStep>();
    }

    /// <summary>
    /// 视觉流程的现成例子（页面左上【举个例子】一键摆好整条流程）。
    ///
    /// ★ 铁律：例子必须**点【运行一次】就能跑通**。没接相机时引擎会退回自带示例图，
    ///   所以例子里的坐标都按那张图（640×480，亮方块在 x 384~490 / y 192~272）预先填好，
    ///   而且每一步给的数要能对上——新手抄完拿到一串红色失败，比没有例子更坑。
    ///   标定那一步用「示例图」模式（DemoCalib 按已知真值画 9 + 5 张），所以不接相机也能看到真实的
    ///   找标记 → 配对 → 拟合过程和落盘的图；它的数不许写进真实相机的标定记录。
    ///   加新例子后必须在 ncm_op_test 里整条跑一遍再提交。
    /// </summary>
    public static class FlowExamples
    {
        static VisualFlowStep Capture(string name) => new VisualFlowStep
        {
            Name = name,
            StepType = "图像采集",
            SourceType = "相机",
            Width = 640,
            Height = 480,
        };

        /// <summary>
        /// 演示用的自动标定步骤：软件自己走 9 个位置（3×3）拍 9 张定 XY，再转 5 个角度拍 5 张定旋转中心。
        /// 勾了「用示例图」→ 不碰相机也不碰轴，图是软件按一套已知真值画的，14 张照样落到 CalibImages 里。
        /// 数字要和后面几步对得上，所以间距 / 步距 / 点数都和 DemoCalib 的真值一致。
        /// </summary>
        static VisualFlowStep DemoCalibStep(string name) => new VisualFlowStep
        {
            Name = name,
            StepType = "标定",
            CalibMode = "XY+旋转",
            CalibDemo = true,
            CalibPitchMm = 10,
            CalibRotationStepDeg = 20,
            CalibRotationCount = 5,
            CalibSettleMs = 0,
            CalibThreshold = 128,
            CalibDarkMarker = true,
            CalibMinArea = 50,
            CalibMaxArea = 4000,
        };

        static VisualFlowStep Op(string name, VisionOpSpec spec, params (string Key, string Value)[] ps)
        {
            var s = new VisualFlowStep { Name = name, StepType = spec.Name, Op = spec.Key };
            foreach (var (k, v) in ps) s.SetOpParam(k, v);
            return s;
        }

        /// <summary>按白话名取算子目录（例子里的名字改了的，编译期就报错，不会静默填错参数）。</summary>
        static VisionOpSpec Spec(string name)
        {
            var s = VisionOperators.All2.FirstOrDefault(o => o.Name == name);
            return s ?? throw new InvalidOperationException("算子目录里没有「" + name + "」");
        }

        public static readonly FlowExample[] All =
        {
            new FlowExample
            {
                Key = "align",
                Name = "对位：先定好换算关系，再看清工件偏了多少，最后算轴该走多少",
                Help = "对位的前提是「图上的 1 像素等于机台的多少毫米」和「转盘绕哪一点转」，所以这条线是六步："
                     + "① 自动标定演示：软件自己走 9 个位置（3×3）拍 9 张定 XY，再转 5 个角度拍 5 张定旋转中心。"
                     + "这步勾了「用示例图」，不接相机、不接轴也能跑完 —— 图是软件按一套已知真值画的，"
                     + "14 张图和「标定配对.csv」会存到程序目录的 CalibImages 里；"
                     + "标出来应当是 0.1 mm/px、旋转中心 (300, 260)，对得上就说明找标记 → 配对 → 拟合这条链是通的。"
                     + "接真机时把这步的「用示例图」取消勾选、填自己的轴名，或者删掉这步改跑真标定。"
                     + "② 拍一张工件照 → ③ 多点标定 9 对（把这条流程的换算关系定住，供⑤⑥用）→ "
                     + "④ 找工件的两个角 → ⑤ 和标准两点比一比，得出 X / Y / 角度偏多少 → ⑥ 算出轴该走多少。"
                     + "数值都按软件自带的示例图（640×480，亮方块在 x 384~490 / y 192~272）填好了，点【运行一次】就能看每步结果。"
                     + "换到自己的机台：主要改④框里的工件、⑤的两个标准点（拿合格件跑一次后点【记为标准】）和⑥的现在位置。",
                Build = () => new[]
                {
                    DemoCalibStep("① 自动拍 9 张定 XY + 5 张定旋转中心"),
                    Capture("② 拍一张照"),
                    Op("③ 定换算关系（9 对）", Spec("多点标定（图上点对机台点）"),
                        ("pairs", "100,80=5,4;300,80=15,4;500,80=25,4;"
                                + "100,240=5,12;300,240=15,12;500,240=25,12;"
                                + "100,400=5,20;300,400=15,20;500,400=25,20"),
                        ("mode", "全算（含倾斜）"),
                        ("tol", "0.05")),
                    Op("④ 找工件的两个角", Spec("找矩形四角"),
                        ("minArea", "2000"), ("canny", "80"), ("approx", "4")),
                    Op("⑤ 量和标准差多少", Spec("两个点对位"),
                        ("src", "上一步的两个角点"),
                        ("b1", "388,195"), ("b2", "388,274"),
                        ("mmpp", "0"), ("tolX", "0.5"), ("tolY", "0.5"), ("tolR", "0.5")),
                    Op("⑥ 算轴该走多少", Spec("算对位移动量"),
                        ("rx", "12"), ("ry", "8"), ("rr", "0"),
                        ("sx", "20"), ("sy", "15"), ("sr", "0"),
                        ("offx", "0"), ("offy", "-5"), ("limit", "10")),
                },
            },
        };

        public static FlowExample Find(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            foreach (var e in All) if (e.Key == key.Trim()) return e;
            return null;
        }

        public static FlowExample FindByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (var e in All) if (e.Name == name.Trim()) return e;
            return null;
        }
    }
}

// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
