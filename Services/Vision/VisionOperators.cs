// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>算子的一个参数（界面按这张表自动生成参数行，不用为每个算子写一遍 XAML）。</summary>
    public sealed class VisionParamDef
    {
        public string Key { get; init; } = "";
        /// <summary>界面上显示的名字（白话）。</summary>
        public string Name { get; init; } = "";
        /// <summary>num=数字 / int=整数 / choice=下拉 / bool=勾选 / roi=框选区域。</summary>
        public string Kind { get; init; } = "num";
        public string Default { get; init; } = "0";
        /// <summary>choice 的候选，用竖线分隔。</summary>
        public string Options { get; init; } = "";
        public double Min { get; init; } = double.MinValue;
        public double Max { get; init; } = double.MaxValue;
        /// <summary>这一行后面的小字说明（鼠标停上去的提示）。</summary>
        public string Help { get; init; } = "";
        /// <summary>界面列宽（像素），roi 要宽一点。</summary>
        public int Width { get; init; } = 96;
    }

    /// <summary>一个视觉算子：白话名字 + 分类 + 参数 + 输出说明。</summary>
    public sealed class VisionOpSpec
    {
        /// <summary>内部键（落盘用，永远不改；改名只改 Name）。</summary>
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        public string Category { get; init; } = "";
        /// <summary>一句话说清它干什么。</summary>
        public string Summary { get; init; } = "";
        /// <summary>它会给出哪些数（结果区与「存进变量」用）。</summary>
        public string Outputs { get; init; } = "";
        public VisionParamDef[] Params { get; init; } = Array.Empty<VisionParamDef>();

        public VisionParamDef Find(string key) => Params.FirstOrDefault(p => p.Key == key);
    }

    /// <summary>
    /// 视觉算子目录（对标 LV Studio 手册第 6 章的视觉工具与海康 VisionMaster 的算子分类）。
    ///
    /// ★ 两条铁律：
    ///   ① Key 是存进工程 xlsx / 节点图 JSON 的值，一旦上线永不改名；要改话术只改 Name。
    ///   ② 这里只登记「真能跑」的算子。没实现的绝不列进来——列表里有、跑起来报未实现，
    ///      比没有这个算子更坑现场。
    ///
    /// 参数键统一用这套名字（roi / cx / cy / angle / length / width / polarity / minGrad /
    /// samples / keep / radius / tol / a0 / a1 / maxHits / minAngle），新算子沿用，别造同义词。
    /// </summary>
    public static class VisionOperators
    {
        static VisionParamDef P(string key, string name, string kind, string def, string help,
            string options = "", double min = double.MinValue, double max = double.MaxValue, int width = 96)
            => new VisionParamDef { Key = key, Name = name, Kind = kind, Default = def, Help = help, Options = options, Min = min, Max = max, Width = width };

        const string PolarityOptions = "任意|暗到亮|亮到暗";

        static readonly VisionParamDef Roi =
            P("roi", "框选区域", "roi", "", "只在这块方框里找（x,y,宽,高）。在右边结果图上按住左键拖一个框就行，不用手填");

        static readonly VisionParamDef[] CommonLine =
        {
            Roi,
            P("cx", "中心X", "num", "0", "搜索带的中心点 X（像素）。0 = 用画面中心"),
            P("cy", "中心Y", "num", "0", "搜索带的中心点 Y（像素）。0 = 用画面中心"),
            P("angle", "边的大概角度", "num", "0", "这条边大概朝哪个方向（度，0 = 水平边）。差得远一点没关系，软件会自己找", min: -180, max: 180),
            P("length", "沿边看多长", "num", "200", "沿着这条边取多长一段（像素）", min: 8, max: 4000),
            P("width", "垂直找多宽", "num", "40", "垂直于边的方向上允许跑多远去找（像素）", min: 4, max: 2000),
            P("polarity", "边的明暗", "choice", "任意", "沿搜索方向（角度再顺时针转 90° 那一侧）看过去：灰度升高选「暗到亮」，降低选「亮到暗」；拿不准就选「任意」，两种都认", PolarityOptions),
            P("minGrad", "边要有多明显", "num", "8", "灰度跳变小于这个数的不算边。越大越严（只认硬边），越小越容易把噪点当边", min: 0.5, max: 200),
            P("samples", "取几条探针", "int", "60", "沿边均匀放多少条探针。越多越稳也越慢（一般 40~120）", min: 5, max: 600),
            P("keep", "相信几成的点", "num", "0.9", "拟线时保留多少比例的点，剩下的当杂点扔掉。杂点多就调小", min: 0.5, max: 1),
        };

        static readonly VisionParamDef[] CommonCircle =
        {
            Roi,
            P("cx", "圆心X", "num", "0", "圆心的大概位置 X（像素）。0 = 用画面中心"),
            P("cy", "圆心Y", "num", "0", "圆心的大概位置 Y（像素）。0 = 用画面中心"),
            P("radius", "半径大概多少", "num", "60", "圆的半径估计值（像素），软件在这个值上下找边", min: 2, max: 5000),
            P("tol", "半径允许差多少", "num", "20", "允许半径比估计值大/小多少（像素）。太大会找到别的圆", min: 1, max: 2000),
            P("polarity", "边的明暗", "choice", "任意", "从圆内往外看，是暗到亮还是亮到暗；不确定选「任意」", PolarityOptions),
            P("minGrad", "边要有多明显", "num", "8", "灰度跳变小于这个数的不算边", min: 0.5, max: 200),
            P("samples", "绕一圈取几个点", "int", "120", "沿圆周放多少条径向探针。越多越稳也越慢（一般 60~240）", min: 8, max: 720),
            P("keep", "相信几成的点", "num", "0.9", "拟圆时保留多少比例的点，剩下的当杂点", min: 0.5, max: 1),
        };

        static readonly VisionOpSpec[] All =
        {
            new VisionOpSpec
            {
                Key = "line", Name = "找直线", Category = "找边与测量",
                Summary = "在框里找一条最明显的直边，给出两端点、角度和准不准。",
                Outputs = "直线起点X/起点Y/终点X/终点Y/角度/偏差(越小越准)/用到的点数",
                Params = CommonLine,
            },
            new VisionOpSpec
            {
                Key = "lines", Name = "找多条直线", Category = "找边与测量",
                Summary = "同一个方向上找出几条平行边（例如三条刻线），逐条给角度和位置。",
                Outputs = "找到的条数/第一条的角度/平均偏差/各条端点",
                Params = CommonLine.Concat(new[]
                {
                    P("maxHits", "最多找几条", "int", "3", "找到这么多条就停", min: 1, max: 20),
                    P("minGap", "两条至少隔多远", "num", "12", "两条边靠得比这还近就当成同一条（像素）", min: 1, max: 500),
                }).ToArray(),
            },
            new VisionOpSpec
            {
                Key = "circle", Name = "找圆", Category = "找边与测量",
                Summary = "在框里找一个圆（孔、焊点、圆片），给出圆心和半径。",
                Outputs = "圆心X/圆心Y/半径/直径/偏差/用到的点数",
                Params = CommonCircle,
            },
            new VisionOpSpec
            {
                Key = "arc", Name = "找圆弧", Category = "找边与测量",
                Summary = "只在一部分角度上找圆边（被挡住的圆、R 角、弧形密封线）。",
                Outputs = "圆心X/圆心Y/半径/起始角/结束角/偏差",
                Params = CommonCircle.Concat(new[]
                {
                    P("a0", "从几度开始找", "num", "0", "只在这个角度范围内找边（度）", min: -360, max: 360),
                    P("a1", "找到几度结束", "num", "360", "和「从几度开始」之间就是要看的扇形区（度）", min: -360, max: 360),
                }).ToArray(),
            },
            new VisionOpSpec
            {
                Key = "edgepoints", Name = "找边缘点", Category = "找边与测量",
                Summary = "沿一条线把路上遇到的每个台阶都列出来（看有几层、间距多少）。",
                Outputs = "边缘点个数/第一个点位置/最后一个点位置/相邻间距",
                Params = new[]
                {
                    Roi,
                    P("x0", "从哪开始X", "num", "0", "剖面起点 X（像素）"),
                    P("y0", "从哪开始Y", "num", "0", "剖面起点 Y（像素）"),
                    P("x1", "到哪结束X", "num", "0", "剖面终点 X（像素）。0 = 从起点水平向右"),
                    P("y1", "到哪结束Y", "num", "0", "剖面终点 Y（像素）"),
                    P("polarity", "边的明暗", "choice", "任意", "只认这个方向的跳变", PolarityOptions),
                    P("minGrad", "边要有多明显", "num", "8", "灰度跳变小于这个数的不算边", min: 0.5, max: 200),
                    P("samples", "取多少个采样点", "int", "200", "这条线上一共采多少个灰度值", min: 16, max: 4000),
                },
            },
            new VisionOpSpec
            {
                Key = "quad", Name = "找矩形四角", Category = "找边与测量",
                Summary = "找出框里那块方形工件的四个角和它的偏转角度（对位、贴合用）。",
                Outputs = "中心X/中心Y/角度/宽/高/四个角坐标",
                Params = new[]
                {
                    Roi,
                    P("minArea", "最小面积", "num", "2000", "比这还小的方块不当目标（像素²），用来滤掉碎块", min: 10, max: 1e9),
                    P("canny", "找边灵敏度", "num", "80", "内阈值。越大越只保留硬边", min: 10, max: 400),
                    P("approx", "角点松紧", "num", "4", "把轮廓折成多边形时的容差（像素）：越大角越少越粗", min: 0.5, max: 40),
                },
            },
            new VisionOpSpec
            {
                Key = "corner", Name = "找尖角", Category = "找边与测量",
                Summary = "找出框里的尖角 / 十字交点（定位角点、判断缺角）。",
                Outputs = "尖角个数/第一个角X/第一个角Y/各角坐标",
                Params = new[]
                {
                    Roi,
                    P("maxCorners", "最多找几个", "int", "10", "找到这么多就停", min: 1, max: 200),
                    P("quality", "角要有多突出", "num", "0.05", "相对强度门槛：越小找到的角越多（也越容易找到假的）", min: 0.005, max: 0.9),
                    P("minDist", "两个角至少隔多远", "num", "20", "靠得比这更近的两个角只留一个（像素）", min: 1, max: 500),
                },
            },
            new VisionOpSpec
            {
                Key = "straight", Name = "看这条边够不够直", Category = "找边与测量",
                Summary = "先按「找直线」找一条边，再报它最大弯了多少（直线度）：数值越小越直。",
                Outputs = "最大偏离/偏差(均方根)/用到的点数/角度",
                Params = CommonLine,
            },
            new VisionOpSpec
            {
                Key = "fitline", Name = "把点连成线", Category = "找边与测量",
                Summary = "拿上一步找到的边缘点拟一条线（先跑「找边缘点」或「找直线」再来这一步）。",
                Outputs = "直线起点X/起点Y/终点X/终点Y/角度/偏差/点数",
                Params = new[]
                {
                    P("keep", "相信几成的点", "num", "0.9", "保留多少比例的点，其余当杂点扔掉", min: 0.5, max: 1),
                },
            },
            new VisionOpSpec
            {
                Key = "fitcircle", Name = "把点拟成圆", Category = "找边与测量",
                Summary = "拿上一步的边缘点拟一个圆（点是从「找边缘点」/「找圆弧」来的）。",
                Outputs = "圆心X/圆心Y/半径/偏差/点数",
                Params = new[]
                {
                    P("keep", "相信几成的点", "num", "0.9", "保留多少比例的点，其余当杂点扔掉", min: 0.5, max: 1),
                },
            },
            // ---------------- 批次 2：几何计算（吃上一步找到的线 / 圆 / 点） ----------------
            new VisionOpSpec
            {
                Key = "pp", Name = "两点多远", Category = "几何计算",
                Summary = "给两个点，算它们之间的距离和连线角度（孔距、引脚间距、长度抽检）。",
                Outputs = "距离/X方向差/Y方向差/角度",
                Params = new[]
                {
                    P("ax", "第一个点X", "num", "0", "第一个点的 X（像素）。想直接用上一步找到的点，把下面「用哪来的点」改成对应项"),
                    P("ay", "第一个点Y", "num", "0", "第一个点的 Y（像素）"),
                    P("bx", "第二个点X", "num", "0", "第二个点的 X（像素）"),
                    P("by", "第二个点Y", "num", "0", "第二个点的 Y（像素）"),
                    P("use", "用哪来的点", "choice", "手填", "手填 = 用上面四个数；上一步的角点 = 直接拿「找矩形四角 / 找尖角」找到的点，省得抄坐标", "手填|上一步的角点|上一步的边缘点"),
                },
            },
            new VisionOpSpec
            {
                Key = "pl", Name = "点离一条线多远", Category = "几何计算",
                Summary = "算一个点到一条直线的垂直距离，并给垂足（边到孔、针到基准线）。",
                Outputs = "垂直距离/垂足X/垂足Y/在线上的位置",
                Params = new[]
                {
                    P("px", "这个点X", "num", "0", "要量的点 X（像素）"),
                    P("py", "这个点Y", "num", "0", "要量的点 Y（像素）"),
                    P("use", "线从哪来", "choice", "上一步的线", "上一步的线 = 用刚「找直线」找到的那条；手填 = 用下面四个数", "上一步的线|手填"),
                    P("x1", "线起点X", "num", "0", "手填时的线起点 X"),
                    P("y1", "线起点Y", "num", "0", "手填时的线起点 Y"),
                    P("x2", "线终点X", "num", "0", "手填时的线终点 X"),
                    P("y2", "线终点Y", "num", "0", "手填时的线终点 Y"),
                },
            },
            new VisionOpSpec
            {
                Key = "pc", Name = "点离一个圆多远", Category = "几何计算",
                Summary = "算一个点到圆心的距离、以及到圆边的距离（正数在圆外，负数在圆内）。",
                Outputs = "到圆心距离/到圆边距离/在圆内还是圆外",
                Params = new[]
                {
                    P("px", "这个点X", "num", "0", "要量的点 X（像素）"),
                    P("py", "这个点Y", "num", "0", "要量的点 Y（像素）"),
                    P("use", "圆从哪来", "choice", "上一步的圆", "上一步的圆 = 用刚「找圆」找到的那个；手填 = 用下面三个数", "上一步的圆|手填"),
                    P("cx", "圆心X", "num", "0", "手填时的圆心 X"),
                    P("cy", "圆心Y", "num", "0", "手填时的圆心 Y"),
                    P("radius", "半径", "num", "0", "手填时的半径（像素）"),
                },
            },
            new VisionOpSpec
            {
                Key = "ll", Name = "两条线的夹角和交点", Category = "几何计算",
                Summary = "拿最近找到的两条线，算它们夹角多大、交点在哪儿（直角边、十字交点）。",
                Outputs = "夹角/交点X/交点Y/是否平行",
                Params = new[]
                {
                    P("tol", "多小算平行", "num", "0.5", "两条线交角小于这个度数就当成平行（只报间距，不报交点）", min: 0.01, max: 45),
                },
            },
            new VisionOpSpec
            {
                Key = "lc", Name = "一条线到一个圆", Category = "几何计算",
                Summary = "算圆心离一条线多远、这条线有没有穿过圆（切线判定、探针与焊点）。",
                Outputs = "圆心到线距离/与半径差/相交情况/交点X/交点Y",
                Params = Array.Empty<VisionParamDef>(),
            },
            new VisionOpSpec
            {
                Key = "cc", Name = "两个圆的间隙", Category = "几何计算",
                Summary = "拿最近找到的两个圆，算圆心距和它们之间的间隙（负数就是重叠了）。",
                Outputs = "圆心距/间隙/半径差/连线角度",
                Params = Array.Empty<VisionParamDef>(),
            },
            new VisionOpSpec
            {
                Key = "gap", Name = "两条平行线间距", Category = "几何计算",
                Summary = "量两条边之间的垂直距离（缝隙、膜宽、印刷线宽）。",
                Outputs = "间距/第一条角度/第二条角度/角度差",
                Params = new[]
                {
                    P("where", "在哪量间距", "num", "0", "在两条线的哪个位置量（0 = 用第一条线的中点，像素）"),
                },
            },
            new VisionOpSpec
            {
                Key = "pxstat", Name = "这块有多亮", Category = "几何计算",
                Summary = "统计框里这片区域的明暗：平均、最亮、最暗、起伏多大（判断反光、脏污、缺料）。",
                Outputs = "平均灰度/最亮/最暗/起伏(标准差)/亮点占比/暗点占比",
                Params = new[]
                {
                    Roi,
                    P("hi", "算亮的门槛", "num", "200", "灰度大于这个数算「亮点」，用来统计白点/脏点比例", min: 1, max: 255),
                    P("lo", "算暗的门槛", "num", "50", "灰度小于这个数算「暗点」", min: 0, max: 254),
                },
            },
            new VisionOpSpec
            {
                Key = "histo", Name = "明暗分布", Category = "几何计算",
                Summary = "把框里的明暗分布列出来，并自动给一个黑白分界值（拿不准阈值填多少时先用它）。",
                Outputs = "建议分界值/最亮档/最暗档/中位数/对比度",
                Params = new[] { Roi },
            },

            // ---------------- 批次 2：标定与换算 ----------------
            new VisionOpSpec
            {
                Key = "mscale", Name = "定比例（像素↔毫米）", Category = "标定与换算",
                Summary = "拿一个已知实际尺寸的标件，算出「1 毫米等于多少像素」，后面量的就是真尺寸。",
                Outputs = "每毫米多少像素/每像素多少毫米",
                Params = new[]
                {
                    P("actual", "标件实际多长", "num", "10", "标准块 / 圆规的实际尺寸（毫米）", min: 0.001, max: 1e6),
                    P("pixels", "图上量到多长", "num", "0", "同一个东西在图上占多少像素（0 = 用上一条算子量到的距离）"),
                    P("method", "量的什么", "choice", "长度", "长度 = 用上一条量到的距离；圆直径 = 用上一条找圆的直径", "长度|圆直径"),
                },
            },
            new VisionOpSpec
            {
                Key = "mcaltwo", Name = "手动对两个点", Category = "标定与换算",
                Summary = "点两个位置：告诉软件它们在图上的像素坐标和在机台上的毫米坐标，算出比例、转角和零点。",
                Outputs = "每毫米多少像素/转角/零点X/零点Y",
                Params = new[]
                {
                    P("p1x", "第1点 图上X", "num", "0", "第一个标记在图上的 X（像素）"),
                    P("p1y", "第1点 图上Y", "num", "0", "第一个标记在图上的 Y（像素）"),
                    P("m1x", "第1点 机台X", "num", "0", "第一个标记在机台上的 X（毫米）"),
                    P("m1y", "第1点 机台Y", "num", "0", "第一个标记在机台上的 Y（毫米）"),
                    P("p2x", "第2点 图上X", "num", "0", "第二个标记在图上的 X（像素）"),
                    P("p2y", "第2点 图上Y", "num", "0", "第二个标记在图上的 Y（像素）"),
                    P("m2x", "第2点 机台X", "num", "0", "第二个标记在机台上的 X（毫米）"),
                    P("m2y", "第2点 机台Y", "num", "0", "第二个标记在机台上的 Y（毫米）"),
                },
            },
            new VisionOpSpec
            {
                Key = "px2mm", Name = "图上位置换算到机台", Category = "标定与换算",
                Summary = "把刚找到的点（圆心 / 角点）换算成机台毫米坐标，流程就能直接走过去取料。",
                Outputs = "机台X/机台Y/每毫米多少像素/转角",
                Params = new[]
                {
                    P("src", "换算哪个点", "choice", "上一步找到的点", "上一步找到的点 = 圆心 / 角点 / 重心；手填 = 用下面两个数", "上一步找到的点|手填"),
                    P("px", "图上X", "num", "0", "手填时的图上 X（像素）"),
                    P("py", "图上Y", "num", "0", "手填时的图上 Y（像素）"),
                    P("cam", "用哪台相机的标定", "text", "", "从「相机」页选一台；留空 = 用最近一次采集的相机"),
                },
            },
            new VisionOpSpec
            {
                Key = "mm2px", Name = "机台位置换算回图上", Category = "标定与换算",
                Summary = "反过来：知道机台坐标，算它在图上的哪个像素位置（预对准、画参考框）。",
                Outputs = "图上X/图上Y",
                Params = new[]
                {
                    P("mx", "机台X", "num", "0", "机台坐标 X（毫米）"),
                    P("my", "机台Y", "num", "0", "机台坐标 Y（毫米）"),
                    P("cam", "用哪台相机的标定", "text", "", "从「相机」页选一台；留空 = 用最近一次采集的相机"),
                },
            },
            new VisionOpSpec
            {
                Key = "scalar", Name = "读数换算成实际值", Category = "标定与换算",
                Summary = "把原始读数（AD 值、脉冲数、格数）按比例和偏移换算成真实物理量。",
                Outputs = "换算后的值/比例/偏移",
                Params = new[]
                {
                    P("raw", "原始读数", "num", "0", "要换算的原始数（0 = 用上一条算子的结果）"),
                    P("scale", "乘多少", "num", "1", "比例：实际值 = 原始值 × 这个数 + 偏移"),
                    P("offset", "再加多少", "num", "0", "偏移（和结果同单位）"),
                    P("unit", "单位叫什么", "text", "mm", "只是给结果显示的单位名，不参与计算"),
                },
            },

            // ---------------- 批次 3：匹配与识别 ----------------
            new VisionOpSpec
            {
                Key = "shape", Name = "照样子找图", Category = "匹配与识别",
                Summary = "给一张样例小图，在大图里找它出现在哪儿、转了多少度（工件转了、光照变了也能找到）。",
                Outputs = "位置X/位置Y/角度/相似度/找到几个",
                Params = new[]
                {
                    P("tpl", "样例图文件", "text", "", "样例图（模板）的完整路径，例如 D:\\模板\\工件.png。也可以在右边结果图上框一块再抠成样例图"),
                    P("angle", "允许转多大角度", "num", "360", "找的时候允许样例转多少度（0 到这个值之间来回试）", min: 0, max: 360),
                    P("minScore", "至少有多像", "num", "0.75", "相似度低于这个数就不算找到（0~1，越高越严）", min: 0.05, max: 1),
                    P("mode", "比什么", "choice", "灰度", "灰度 = 连明暗一起比；只看轮廓 = 只比形状，光照不均时更稳", "灰度|只看轮廓"),
                    P("maxHits", "最多找几个", "int", "10", "图里有多个同样的工件时，最多报几个", min: 1, max: 60),
                },
            },
            new VisionOpSpec
            {
                Key = "feat", Name = "找特征点对齐", Category = "匹配与识别",
                Summary = "靠图上的特征点对齐两张图，给出平移多少、转了多少度、放大多少（工件随便摆也能对上）。",
                Outputs = "平移X/平移Y/转角/缩放/对上的特征点数/站住的点数/平均偏差/最大偏差",
                Params = new[]
                {
                    P("tpl", "样例图文件", "text", "", "样例图（模板）的完整路径"),
                    P("minPairs", "至少对上几个点", "int", "6", "对上的特征点少于这个数就判失败（太少说明是巧合）", min: 3, max: 100),
                    P("maxFeats", "最多取几个特征点", "int", "600", "图越花可以取越多；对不上点时试着加大（改动会自动重算，不用改图）", min: 50, max: 3000),
                    P("tol", "算几个像素以内算对上", "num", "3", "配对点落进这个圈才算数；图纸对不上就调大一点（越大越容易把巧合也算进来）", min: 0.5, max: 30),
                },
            },
            new VisionOpSpec
            {
                Key = "blob", Name = "按面积筛斑点", Category = "匹配与识别",
                Summary = "把亮于 / 暗于某个灰度值的块找出来并按面积筛，报个数、总面积和重心（有无焊锡、缺胶）。",
                Outputs = "斑点个数/总面积/最大面积/重心X/重心Y/合格与否",
                Params = new[]
                {
                    Roi,
                    P("thr", "分界灰度", "num", "128", "大于它算亮斑，小于它算暗斑（看下面选哪个）", min: 1, max: 254),
                    P("which", "找哪种", "choice", "亮斑", "亮斑 = 找比阈值亮的；暗斑 = 找比阈值暗的", "亮斑|暗斑"),
                    P("minArea", "太小不算", "num", "30", "面积小于这个像素数的碎点忽略（滤噪点）", min: 1, max: 1e7),
                    P("maxArea", "太大不算", "num", "0", "面积大于这个像素数的当背景忽略（0 = 不限制）", min: 0, max: 1e8),
                    P("expect", "应该有几个", "num", "0", "期望的个数；填 0 = 不判定只报数", min: 0, max: 10000),
                },
            },
            new VisionOpSpec
            {
                Key = "regions", Name = "数有几块", Category = "匹配与识别",
                Summary = "把图分成一块一块并数出来，报每块多大（颗粒计数、胶点个数、孔数）。",
                Outputs = "块数/最大块面积/平均块面积/总面积",
                Params = new[]
                {
                    Roi,
                    P("thr", "分界灰度", "num", "128", "灰度大于它算一块", min: 1, max: 254),
                    P("minArea", "太小不算", "num", "20", "面积小于这个像素数的碎块忽略", min: 1, max: 1e7),
                },
            },
            new VisionOpSpec
            {
                Key = "bar1d", Name = "扫条形码", Category = "匹配与识别",
                Summary = "读图里的一维条形码（Code128 / EAN / 39 这些），把内容和位置报出来。",
                Outputs = "内容/码制/个数/中心X/中心Y/合格与否",
                Params = new[]
                {
                    Roi,
                    P("expect", "期望内容", "text", "", "填了就比对：不一致这一步算不合格；留空 = 只读不判"),
                    P("how", "怎么比对", "choice", "完全一样", "包含 = 只要含有这段就算对", "完全一样|包含|开头是"),
                },
            },
            new VisionOpSpec
            {
                Key = "qr", Name = "扫二维码", Category = "匹配与识别",
                Summary = "读图里的二维码（一个或多个），把内容和四个角报出来。",
                Outputs = "内容/个数/中心X/中心Y/合格与否",
                Params = new[]
                {
                    Roi,
                    P("expect", "期望内容", "text", "", "填了就比对；留空 = 只读不判"),
                    P("how", "怎么比对", "choice", "完全一样", "包含 = 只要含有这段就算对", "完全一样|包含|开头是"),
                },
            },
            new VisionOpSpec
            {
                Key = "codecheck", Name = "读码并判合格", Category = "匹配与识别",
                Summary = "一维码二维码都试一遍，读到并且和期望一致才算合格（防混料、防贴错标）。",
                Outputs = "内容/码类型/合格与否",
                Params = new[]
                {
                    Roi,
                    P("expect", "期望内容", "text", "", "必须读到的内容（比对方式见下）"),
                    P("how", "怎么比对", "choice", "完全一样", "完全一样 / 包含 / 开头是", "完全一样|包含|开头是"),
                    P("allowEmpty", "读不到算合格吗", "bool", "0", "勾选 = 读不到也放过（只做记录）；默认不勾 = 读不到就判不合格"),
                },
            },

            // ---------------- 批次 4：图像与颜色 ----------------
            new VisionOpSpec
            {
                Key = "colorspace", Name = "换一种看法", Category = "图像与颜色",
                Summary = "把图换成 HSV / Lab / 灰度等表示方式（按颜色找东西时，先换成 HSV 更好分）。",
                Outputs = "换成了哪种",
                Params = new[]
                {
                    P("to", "换成哪种", "choice", "HSV", "HSV = 按色调/饱和/明度分色最好用；Lab = 更接近人眼感觉；灰度 = 只留明暗", "HSV|Lab|灰度|回到彩色(BGR)"),
                },
            },
            new VisionOpSpec
            {
                Key = "chan", Name = "只留一种颜色", Category = "图像与颜色",
                Summary = "只保留一个通道的信息（例如只留红色那一层，红色工件和绿色背景就分开了）。",
                Outputs = "留了哪个通道",
                Params = new[]
                {
                    P("ch", "留哪个", "choice", "H(色调)", "H/S/V 要先换成 HSV 才有意义；R/G/B 是彩色图的三个分量", "H(色调)|S(饱和)|V(明度)|R|G|B"),
                },
            },
            new VisionOpSpec
            {
                Key = "colorchk", Name = "这块颜色对不对", Category = "图像与颜色",
                Summary = "量框里这片颜色的平均色，和期望值比，差太多就判不合格（喷漆、标识色、线色）。",
                Outputs = "平均H/平均S/平均V/最大偏差/合格与否",
                Params = new[]
                {
                    Roi,
                    P("eh", "期望色调H", "num", "0", "期望颜色的色调（0~180）", min: 0, max: 180),
                    P("es", "期望饱和S", "num", "0", "期望颜色的饱和度（0~255）", min: 0, max: 255),
                    P("ev", "期望明度V", "num", "200", "期望颜色的明度（0~255）", min: 0, max: 255),
                    P("tol", "允许差多少", "num", "30", "三项里任何一项超过这个差值就判不合格", min: 1, max: 255),
                },
            },
            new VisionOpSpec
            {
                Key = "enhance", Name = "把对比拉开", Category = "图像与颜色",
                Summary = "让暗处亮起来、灰蒙蒙的图变分明，后面找边找模板都更稳。",
                Outputs = "用的哪种/增强前对比度/增强后对比度",
                Params = new[]
                {
                    P("how", "用哪种", "choice", "局部增强(CLAHE)", "局部增强 = 分块拉对比，最适合光照不均；整体拉伸 = 把明暗范围撑满；提亮 / 压暗 = 按伽马调", "局部增强(CLAHE)|整体拉伸|提亮|压暗"),
                    P("strength", "使多大劲", "num", "2", "越大越猛（局部增强 1~10；提亮压暗 0.2~5）", min: 0.2, max: 10),
                },
            },
            new VisionOpSpec
            {
                Key = "denoise", Name = "把图洗干净", Category = "图像与颜色",
                Summary = "去掉麻点噪声，同时尽量不把边抹糊（磨削面、印刷面先洗一遍再量）。",
                Outputs = "用的哪种/范围多大",
                Params = new[]
                {
                    P("how", "用哪种", "choice", "双边(保边)", "双边 = 抹平噪声但保留硬边；中值 = 去麻点最狠；高斯 = 最温和", "双边(保边)|中值|高斯"),
                    P("size", "范围多大", "int", "5", "影响的像素范围，要用奇数（3/5/7…）；越大越糊", min: 1, max: 51),
                },
            },
            new VisionOpSpec
            {
                Key = "geom", Name = "挪动 / 转 / 翻这块图", Category = "图像与颜色",
                Summary = "把当前图平移、旋转、缩放或镜像（对齐参考图、把倒放的工件摆正再量）。",
                Outputs = "平移X/平移Y/转角/缩放/镜像",
                Params = new[]
                {
                    P("dx", "左右挪多少", "num", "0", "正数往右挪（像素）"),
                    P("dy", "上下挪多少", "num", "0", "正数往下挪（像素）"),
                    P("angle", "转多少度", "num", "0", "绕中心旋转的角度（度，正=顺时针）", min: -360, max: 360),
                    P("scale", "放大多少倍", "num", "1", "1 = 原大小；2 = 放大一倍", min: 0.1, max: 10),
                    P("flip", "翻面", "choice", "不翻", "左右翻 = 水平镜像；上下翻 = 垂直镜像", "不翻|左右翻|上下翻"),
                },
            },
            new VisionOpSpec
            {
                Key = "save", Name = "存成图片文件", Category = "图像与颜色",
                Summary = "把现在这张图（含算子画的框和点）存成文件，留证据给追溯用。",
                Outputs = "存到哪了/是否成功",
                Params = new[]
                {
                    P("path", "存到哪", "text", "out\\视觉.jpg", "完整路径或相对路径都行；文件名里带 % 会按时间戳自动编号"),
                    P("withMark", "要不要带标注", "bool", "1", "勾选 = 存带框和点的图；不勾 = 存干净的原图"),
                },
            },
            new VisionOpSpec
            {
                Key = "load", Name = "读一张图进来", Category = "图像与颜色",
                Summary = "不走相机，直接从文件读一张图当当前图像（拿历史照片复现问题、离线调参数）。",
                Outputs = "读的文件/宽/高",
                Params = new[]
                {
                    P("path", "读哪个文件", "text", "", "图片完整路径，例如 D:\\图\\bad01.png"),
                },
            },
            new VisionOpSpec
            {
                Key = "twoimg", Name = "两张图做运算", Category = "图像与颜色",
                Summary = "拿当前图和另一张图做加减或与或（和标准图对比找差异、把两张不同曝光的合起来）。",
                Outputs = "用的哪种运算/另一张图",
                Params = new[]
                {
                    P("second", "另一张图", "text", "", "另一张图的完整路径"),
                    P("op", "怎么算", "choice", "相减找差异", "相加 = 变亮合并；相减 = 看差异；都亮的留 = 交集；有一个亮就留 = 并集", "相加|相减找差异|都亮的留|有一个亮就留|取绝对差"),
                },
            },
            new VisionOpSpec
            {
                Key = "crop", Name = "只留下框住的那块", Category = "图像与颜色",
                Summary = "把框选区域抠出来当新图，后面所有算子只在这块里跑（省时间，也防误检）。",
                Outputs = "框X/框Y/宽/高",
                Params = new[] { Roi },
            },
        };

        /// <summary>全部算子（界面分组显示、执行器按 Key 找实现）。</summary>
        public static IReadOnlyList<VisionOpSpec> All2 => All;

        public static VisionOpSpec Find(string key)
            => string.IsNullOrWhiteSpace(key) ? null : All.FirstOrDefault(a => a.Key == key.Trim());

        /// <summary>界面上的算子下拉：按分类分组的「分类 / 算子名」列表。</summary>
        public static IReadOnlyList<VisionOpSpec> ByCategory(string category)
            => All.Where(a => a.Category == category).ToList();

        public static IReadOnlyList<string> Categories
            => All.Select(a => a.Category).Distinct().ToList();

        /// <summary>所有算子的白话名（守卫与提示词用它核对界面候选）。</summary>
        public static IReadOnlyList<string> Names => All.Select(a => a.Name).ToList();
    }
}

// === NoCodeMotion 视觉流程引擎（OpenCV 实现） | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
