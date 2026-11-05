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
                Name = "对位：看清工件偏了多少，再算轴该走多少",
                Help = "五步一条线：拍一张照 → 定「图上位置 ↔ 机台毫米」的换算关系 → 找出工件上两个角 → "
                     + "和标准位置比一比 → 给出 X / Y / 角度各走多少。"
                     + "坐标和数值都按软件自带示例图填好了，直接点【运行一次】就能看每步结果；"
                     + "接上真相机或换工件后，主要改③的框、④的标准两点和⑤的现在位置。",
                Build = () => new[]
                {
                    Capture("① 拍一张照"),
                    Op("② 定换算关系", Spec("多点标定（图上点对机台点）"),
                        ("pairs", "100,100=5,5;300,100=15,5;100,300=5,15;300,300=15,15"),
                        ("mode", "全算（含倾斜）"),
                        ("tol", "0.05")),
                    Op("③ 找工件的两个角", Spec("找矩形四角"),
                        ("minArea", "2000"), ("canny", "80"), ("approx", "4")),
                    Op("④ 量和标准差多少", Spec("两个点对位"),
                        ("src", "上一步的两个角点"),
                        ("b1", "388,195"), ("b2", "388,274"),
                        ("mmpp", "0"), ("tolX", "0.5"), ("tolY", "0.5"), ("tolR", "0.5")),
                    Op("⑤ 算轴该走多少", Spec("算对位移动量"),
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
