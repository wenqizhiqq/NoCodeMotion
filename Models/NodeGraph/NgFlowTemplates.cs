// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温⁣启⁠志⁣◆​编⁣写‍◇​微‌信​﹕​1‏8⁠7‍◆‏1⁣9‏3⁣6‏◇⁠1‏3‎9⁣9‏　‌※‍保⁣留⁣所‏有‎权‍利⁠请‏勿​删‏除‏◇
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨ۤ
using System;
using System.Collections.Generic;

namespace NoCodeMotion.Models.NodeGraph
{
    /// <summary>一条现成的流程模板：内部键 + 白话名 + 说明 + 造出一张节点图。</summary>
    public sealed class NgFlowTemplate
    {
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary>这条流程在干什么、哪几格必须自己改（界面上直接显示，不靠悬停）。</summary>
        public string Help { get; init; } = "";
        public Func<NgDoc> Build { get; init; } = () => new NgDoc();
    }

    /// <summary>
    /// 节点图的现成流程模板（流程页【举个流程】一键铺到画布）。
    ///
    /// 为什么要模板：对位 / 贴合真正的门槛不是某个算子，而是**整条顺序与互锁** ——
    /// 少一个「判定」就会带病压合，少一个「等停稳」就会撞件。从空白画布开始的人一定会漏。
    ///
    /// 约定：
    ///   · 轴名 / 信号名 / 通讯名 / 相机名一律留默认或示例名，工程里叫什么就改成什么；
    ///   · 视觉算子节点的「变量前缀」定死了它写出的变量名，下游用 ${前缀+数名} 引用（改前缀要一起改引用）；
    ///   · 报文 / 指令里也支持 ${变量名}，机器人直接拿到换算好的毫米数，不用二次计算；
    ///   · 机械手自己动的那套不归本软件管时，两边只走逻辑信号：【打开/关闭输出】举就绪、【发消息】通知、
    ///     【等对方发信号】收应答、【等输入信号】收完成 —— 谁没回信号就停在哪一步，不猜。
    /// </summary>
    public static class NgFlowTemplates
    {
        static NgNode N(NgKind kind, double x, double y, params (string Name, string Value)[] ps)
        {
            var n = new NgNode { Kind = kind, X = x, Y = y };
            var def = NgNodeDefinitions.All.TryGetValue(kind, out var d) ? d : null;
            if (def != null)
                foreach (var pd in def.Props)
                    n.Props.Add(new NgProp { Name = pd.Name, Value = pd.Default ?? "", Options = pd.Options });
            foreach (var (name, value) in ps)
            {
                var p = n.Props.Find(q => q.Name == name);
                if (p == null) n.Props.Add(new NgProp { Name = name, Value = value });
                else p.Value = value;
            }
            return n;
        }

        /// <summary>视觉算子节点：先把「算子 + 变量前缀」定好，再补该算子自己的参数（白话名）。</summary>
        static NgNode Op(double x, double y, string opName, string prefix, params (string, string)[] ps)
        {
            var list = new List<(string, string)> { ("算子", opName), ("变量前缀", prefix) };
            list.AddRange(ps);
            return N(NgKind.VisionOp, x, y, list.ToArray());
        }

        static void L(NgDoc doc, NgNode from, string port, NgNode to)
            => doc.Connections.Add(new NgConnection { SourceId = from.Id, SourcePort = port, TargetId = to.Id });

        static void Out(NgDoc doc, NgNode from, NgNode to) => L(doc, from, "Out", to);

        // ================= 单机：对位纠偏一条线 =================

        static NgDoc AlignLine()
        {
            var doc = new NgDoc();
            const double x0 = 60, dx = 208, yMain = 150, yBad = 348;

            var start = N(NgKind.Start, x0 + dx * 0, yMain);
            var trig = N(NgKind.WaitInput, x0 + dx * 1, yMain, ("信号", ""), ("状态", "高电平"));
            var shot = N(NgKind.CamCapture, x0 + dx * 2, yMain);
            var find = Op(x0 + dx * 3, yMain, "找矩形四角", "找角");
            var align = Op(x0 + dx * 4, yMain, "两个点对位", "对位",
                ("实测两点哪来", "上一步的两个角点"), ("标准第1点", ""), ("标准第2点", ""),
                ("X 最多容多少", "0.2"), ("Y 最多容多少", "0.2"), ("角度最多容多少度", "0.2"));
            var move = Op(x0 + dx * 5, yMain, "算对位移动量", "纠偏",
                ("现在机台X", "0"), ("现在机台Y", "0"), ("标准位置X", "0"), ("标准位置Y", "0"),
                ("吸嘴离相机Y", "0"), ("最多允许走多少毫米", "10"));
            var judge = N(NgKind.Decision, x0 + dx * 6, yMain,
                ("分支数", "2"), ("条件1类型", "变量"), ("条件1名称", "对位是否合格"),
                ("条件1比较", "等于"), ("条件1值", "1"));
            var mvX = N(NgKind.MoveAxis, x0 + dx * 7, yMain,
                ("轴", "X"), ("模式", "绝对"), ("目标位置", "${纠偏目标X}"), ("等待到位", "是"));
            var mvY = N(NgKind.MoveAxis, x0 + dx * 8, yMain,
                ("轴", "Y"), ("模式", "绝对"), ("目标位置", "${纠偏目标Y}"), ("等待到位", "是"));
            var mvR = N(NgKind.MoveAxis, x0 + dx * 9, yMain,
                ("轴", "R"), ("模式", "绝对"), ("目标位置", "${纠偏目标角度}"), ("等待到位", "是"));
            var settle = N(NgKind.WaitAxis, x0 + dx * 10, yMain, ("轴", "R"));
            var recheck = N(NgKind.CamCapture, x0 + dx * 11, yMain);
            var realign = Op(x0 + dx * 12, yMain, "照基准比（一个Mark就够）", "复核");
            var press = N(NgKind.Cylinder, x0 + dx * 13, yMain, ("气缸", ""), ("动作", "伸出"));
            var back = N(NgKind.PointGo, x0 + dx * 14, yMain, ("点位表", ""), ("点位", ""));
            var end = N(NgKind.End, x0 + dx * 15, yMain);

            // 不合格那一支：只开报警，绝不往下走贴合
            var alarm = N(NgKind.IoWrite, x0 + dx * 6, yBad, ("输出", ""), ("值", "1"));
            var stopBad = N(NgKind.End, x0 + dx * 7, yBad);

            doc.Nodes.AddRange(new[]
            {
                start, trig, shot, find, align, move, judge, mvX, mvY, mvR, settle,
                recheck, realign, press, back, end, alarm, stopBad
            });

            Out(doc, start, trig); Out(doc, trig, shot); Out(doc, shot, find);
            Out(doc, find, align); Out(doc, align, move); Out(doc, move, judge);
            L(doc, judge, "条件1", mvX);            // 合格：纠偏 → 复核 → 贴合
            L(doc, judge, "否则", alarm);            // 不合格：报警 + 结束（不贴合）
            Out(doc, mvX, mvY); Out(doc, mvY, mvR); Out(doc, mvR, settle);
            Out(doc, settle, recheck); Out(doc, recheck, realign);
            Out(doc, realign, press); Out(doc, press, back); Out(doc, back, end);
            Out(doc, alarm, stopBad);
            return doc;
        }

        // ================= 整线：双皮带 + 4 相机 + 机构取料 + 机械手信号交互 =================

        /// <summary>
        /// 一条「动作很简单、但互锁一个不能少」的整线：
        ///   · 左皮带来一件料 → 相机1 看有没有 / 正反面 → 相机2 定位 → 机构（X/Y + 吸嘴 + 真空）取起来；
        ///   · 走到机械手取料位 → 相机3 确认这件料放到位了，才举「视觉就绪」并给一条 V_READY；
        ///   · 机械手怎么动由它自己的控制器管，本软件只等它的信号：V_ACK（可以放）→ 破真空放料
        ///     → 「机械手取料完成」→「机械手放料完成」（它把成品放到右皮带）；
        ///   · 相机4 复核右皮带上这件成品 → 合格才放行、产量+1。
        /// 每条不合格的路都只报警 + NG 计数 + 叫停，绝不带着没确认的信号继续往下走。
        /// 两条皮带在并行段里同时送料（左取料位补料、右放料位腾空），汇合后才开始取放。
        /// </summary>
        static NgDoc CellLine()
        {
            var doc = new NgDoc();
            const double x0 = 60, dx = 190;
            const double yBeltA = 60, yMain = 400, yBeltB = 740;      // 左皮带一行、主线一行、右皮带一行
            const double yBad1 = 1000, yBad2 = 1180;                   // 不合格处理链两排（按列错开）
            double C(int k) => x0 + dx * k;

            // ---- 开工前两道门：安全回路 + 机械手在线（心跳也是信号，不看它怎么动）----
            var start = N(NgKind.Start, C(0), yMain);
            var safe = N(NgKind.WaitInput, C(1), yMain,
                ("信号", "安全回路"), ("状态", "高电平"), ("超时ms", "2000"));
            var beat = N(NgKind.WaitComm, C(2), yMain,
                ("通道", "机械手信号"), ("要等内容", "HEARTBEAT"), ("超时ms", "3000"), ("存到变量", "机械手在线"));
            var jOnline = N(NgKind.Decision, C(3), yMain,
                ("分支数", "2"), ("条件1类型", "变量"), ("条件1名称", "机械手在线"),
                ("条件1比较", "等于"), ("条件1值", "1"));
            var lampGo = N(NgKind.IoWrite, C(4), yMain, ("输出", "绿灯"), ("值", "1"));
            var lampNg = N(NgKind.IoWrite, C(5), yMain, ("输出", "报警灯"), ("值", "0"));

            // ---- 并行段：两条皮带同时送料（左皮带补到取料位、右皮带腾出放料位）----
            var fork = N(NgKind.ParallelFork, C(6), yMain, ("分支数", "2"));
            var aRun = N(NgKind.IoWrite, C(7), yBeltA, ("输出", "皮带左跑"), ("值", "1"));
            var aIn = N(NgKind.WaitInput, C(8), yBeltA,
                ("信号", "左皮带来料到位"), ("状态", "高电平"), ("超时ms", "8000"));
            var aStop = N(NgKind.IoWrite, C(9), yBeltA, ("输出", "皮带左跑"), ("值", "0"));
            var aBlock = N(NgKind.Cylinder, C(10), yBeltA, ("气缸", "左挡停"), ("动作", "伸出"));

            var bRun = N(NgKind.IoWrite, C(7), yBeltB, ("输出", "皮带右跑"), ("值", "1"));
            var bFree = N(NgKind.WaitInput, C(8), yBeltB,
                ("信号", "右皮带放料位空"), ("状态", "高电平"), ("超时ms", "8000"));
            var bStop = N(NgKind.IoWrite, C(9), yBeltB, ("输出", "皮带右跑"), ("值", "0"));

            var join = N(NgKind.ParallelJoin, C(11), yMain, ("分支数", "2"));

            // ---- 相机1：来料有没有、正反对不对（数黑块，数量不对就是不合规）----
            var shotHas = N(NgKind.CamCapture, C(12), yMain, ("相机", "来料相机"));
            var opHas = Op(C(13), yMain, "按面积筛斑点", "来料",
                ("分界灰度", "100"), ("太小不算", "800"), ("应该有几个", "1"));
            var jHas = N(NgKind.Decision, C(14), yMain,
                ("分支数", "2"), ("条件1类型", "变量"), ("条件1名称", "来料合格与否"),
                ("条件1比较", "等于"), ("条件1值", "1"));

            // ---- 相机2：取料定位 → 找样 → 算移动量（超差就不许走位，防撞）----
            var shotPick = N(NgKind.CamCapture, C(15), yMain, ("相机", "取料定位相机"));
            var opFind = Op(C(16), yMain, "照样子找图", "取料", ("至少有多像", "0.7"), ("最多找几个", "1"));
            var jFind = N(NgKind.Decision, C(17), yMain,
                ("分支数", "2"), ("条件1类型", "变量"), ("条件1名称", "取料找到几个"),
                ("条件1比较", "大于等于"), ("条件1值", "1"));
            var opMove = Op(C(18), yMain, "算对位移动量", "取料纠偏",
                ("标准位置X", "0"), ("标准位置Y", "0"), ("吸嘴离相机Y", "-45"), ("最多允许走多少毫米", "12"));
            var jRange = N(NgKind.Decision, C(19), yMain,
                ("分支数", "2"), ("条件1类型", "变量"), ("条件1名称", "取料纠偏在允许范围"),
                ("条件1比较", "等于"), ("条件1值", "1"));

            // ---- 机构取料：走位 → 吸嘴降 → 抽真空 → 等吸稳 → 升 → 挡停放开 → 左皮带送下一件 ----
            var mvX = N(NgKind.MoveAxis, C(20), yMain,
                ("轴", "X"), ("模式", "绝对"), ("目标位置", "${取料纠偏目标X}"), ("等待到位", "是"));
            var mvY = N(NgKind.MoveAxis, C(21), yMain,
                ("轴", "Y"), ("模式", "绝对"), ("目标位置", "${取料纠偏目标Y}"), ("等待到位", "是"));
            var down = N(NgKind.Cylinder, C(22), yMain, ("气缸", "取料吸嘴"), ("动作", "伸出"));
            var settle = N(NgKind.Delay, C(23), yMain, ("时间ms", "150"));
            var vacOn = N(NgKind.IoWrite, C(24), yMain, ("输出", "真空吸"), ("值", "1"));
            var suck = N(NgKind.Delay, C(25), yMain, ("时间ms", "250"));
            var up = N(NgKind.Cylinder, C(26), yMain, ("气缸", "取料吸嘴"), ("动作", "缩回"));
            var unBlock = N(NgKind.Cylinder, C(27), yMain, ("气缸", "左挡停"), ("动作", "缩回"));
            var aNext = N(NgKind.IoWrite, C(28), yMain, ("输出", "皮带左跑"), ("值", "1"));

            // ---- 走到机械手取料位，相机3 确认这件料到位了才许举「就绪」----
            var toPut = N(NgKind.PointGo, C(29), yMain, ("点位表", "取放工位"), ("点位", "机械手取料位"));
            var shotPut = N(NgKind.CamCapture, C(30), yMain, ("相机", "机械手取料位相机"));
            var opPutFind = Op(C(31), yMain, "照样子找图", "放料位", ("至少有多像", "0.7"), ("最多找几个", "1"));
            var opPutChk = Op(C(32), yMain, "照基准比（一个Mark就够）", "放料确认",
                ("X 最多容多少", "0.5"), ("Y 最多容多少", "0.5"), ("角度最多容多少度", "1"));
            var jPut = N(NgKind.Decision, C(33), yMain,
                ("分支数", "2"), ("条件1类型", "变量"), ("条件1名称", "放料确认是否合格"),
                ("条件1比较", "等于"), ("条件1值", "1"));

            // ---- 逻辑信号交互（硬线一条 + 网口一条，两边都对齐了才放料）----
            var readyOn = N(NgKind.IoWrite, C(34), yMain, ("输出", "视觉就绪"), ("值", "1"));
            var sigReady = N(NgKind.TcpSend, C(35), yMain, ("端点", "127.0.0.1:9003"), ("报文", "V_READY"));
            var ack = N(NgKind.WaitComm, C(36), yMain,
                ("通道", "机械手信号"), ("要等内容", "V_ACK"), ("超时ms", "5000"), ("存到变量", "机械手应答"));
            var jAck = N(NgKind.Decision, C(37), yMain,
                ("分支数", "2"), ("条件1类型", "变量"), ("条件1名称", "机械手应答"),
                ("条件1比较", "等于"), ("条件1值", "1"));

            // ---- 放料：先吹气破真空，等件落稳，再把「就绪」放掉（不放掉对方会当成还要取）----
            var blowOn = N(NgKind.IoWrite, C(38), yMain, ("输出", "吹气"), ("值", "1"));
            var vacOff = N(NgKind.IoWrite, C(39), yMain, ("输出", "真空吸"), ("值", "0"));
            var drop = N(NgKind.Delay, C(40), yMain, ("时间ms", "200"));
            var blowOff = N(NgKind.IoWrite, C(41), yMain, ("输出", "吹气"), ("值", "0"));
            var readyOff = N(NgKind.IoWrite, C(42), yMain, ("输出", "视觉就绪"), ("值", "0"));

            // ---- 等机械手自己完成（我们不管它怎么走），完成后它把成品放到右皮带 ----
            var picked = N(NgKind.WaitInput, C(43), yMain,
                ("信号", "机械手取料完成"), ("状态", "高电平"), ("超时ms", "15000"));
            var placed = N(NgKind.WaitInput, C(44), yMain,
                ("信号", "机械手放料完成"), ("状态", "高电平"), ("超时ms", "20000"));

            // ---- 相机4：成品复核（右皮带上这件有没有、正不正）→ 合格才放行 ----
            var shotEnd = N(NgKind.CamCapture, C(45), yMain, ("相机", "成品复核相机"));
            var opEnd = Op(C(46), yMain, "按面积筛斑点", "复核",
                ("分界灰度", "100"), ("太小不算", "800"), ("应该有几个", "1"));
            var jEnd = N(NgKind.Decision, C(47), yMain,
                ("分支数", "2"), ("条件1类型", "变量"), ("条件1名称", "复核合格与否"),
                ("条件1比较", "等于"), ("条件1值", "1"));
            var bRun2 = N(NgKind.IoWrite, C(48), yMain, ("输出", "皮带右跑"), ("值", "1"));
            var bOut = N(NgKind.WaitInput, C(49), yMain,
                ("信号", "右皮带放行到位"), ("状态", "高电平"), ("超时ms", "8000"));
            var bStop2 = N(NgKind.IoWrite, C(50), yMain, ("输出", "皮带右跑"), ("值", "0"));
            var cnt = N(NgKind.VarSet, C(51), yMain, ("变量", "产量"), ("值", "产量+1"));
            var end = N(NgKind.End, C(52), yMain);

            // ---- 每一道门的不合格支：报警 / NG 计数 / 叫停，绝不带着没确认的信号往下走 ----
            Bad(doc, jOnline,
                N(NgKind.IoWrite, C(4), yBad1, ("输出", "报警灯"), ("值", "1")),
                N(NgKind.IoWrite, C(5), yBad1, ("输出", "绿灯"), ("值", "0")),
                N(NgKind.End, C(6), yBad1));
            Bad(doc, jHas,
                N(NgKind.IoWrite, C(15), yBad1, ("输出", "报警灯"), ("值", "1")),
                N(NgKind.VarSet, C(16), yBad1, ("变量", "NG计数"), ("值", "NG计数+1")),
                N(NgKind.TcpSend, C(17), yBad1, ("端点", "127.0.0.1:9003"), ("报文", "V_NOGOOD")),
                N(NgKind.End, C(18), yBad1));
            Bad(doc, jFind,
                N(NgKind.IoWrite, C(18), yBad2, ("输出", "报警灯"), ("值", "1")),
                N(NgKind.VarSet, C(19), yBad2, ("变量", "NG计数"), ("值", "NG计数+1")),
                N(NgKind.End, C(20), yBad2));
            Bad(doc, jRange,
                N(NgKind.IoWrite, C(20), yBad1, ("输出", "报警灯"), ("值", "1")),
                N(NgKind.VarSet, C(21), yBad1, ("变量", "NG计数"), ("值", "NG计数+1")),
                N(NgKind.End, C(22), yBad1));
            // 位置没确认就不举「就绪」：机械手按自己的坐标来取，取不到件会撞吸嘴
            Bad(doc, jPut,
                N(NgKind.IoWrite, C(34), yBad1, ("输出", "报警灯"), ("值", "1")),
                N(NgKind.VarSet, C(35), yBad1, ("变量", "NG计数"), ("值", "NG计数+1")),
                N(NgKind.TcpSend, C(36), yBad1, ("端点", "127.0.0.1:9003"), ("报文", "V_HOLD")),
                N(NgKind.End, C(37), yBad1));
            // 对方没应答 = 它不打算来取：保持真空别放料，把「就绪」放掉并叫停
            Bad(doc, jAck,
                N(NgKind.IoWrite, C(38), yBad2, ("输出", "视觉就绪"), ("值", "0")),
                N(NgKind.IoWrite, C(39), yBad2, ("输出", "报警灯"), ("值", "1")),
                N(NgKind.TcpSend, C(40), yBad2, ("端点", "127.0.0.1:9003"), ("报文", "V_STOP")),
                N(NgKind.End, C(41), yBad2));
            Bad(doc, jEnd,
                N(NgKind.IoWrite, C(48), yBad2, ("输出", "报警灯"), ("值", "1")),
                N(NgKind.VarSet, C(49), yBad2, ("变量", "NG计数"), ("值", "NG计数+1")),
                N(NgKind.TcpSend, C(50), yBad2, ("端点", "127.0.0.1:9003"), ("报文", "V_NG")),
                N(NgKind.IoWrite, C(51), yBad2, ("输出", "皮带右跑"), ("值", "0")),
                N(NgKind.End, C(52), yBad2));

            doc.Nodes.AddRange(new[]
            {
                start, safe, beat, jOnline, lampGo, lampNg,
                fork, aRun, aIn, aStop, aBlock, bRun, bFree, bStop, join,
                shotHas, opHas, jHas, shotPick, opFind, jFind, opMove, jRange,
                mvX, mvY, down, settle, vacOn, suck, up, unBlock, aNext,
                toPut, shotPut, opPutFind, opPutChk, jPut,
                readyOn, sigReady, ack, jAck,
                blowOn, vacOff, drop, blowOff, readyOff,
                picked, placed,
                shotEnd, opEnd, jEnd, bRun2, bOut, bStop2, cnt, end,
            });

            Out(doc, start, safe); Out(doc, safe, beat); Out(doc, beat, jOnline);
            L(doc, jOnline, "条件1", lampGo); Out(doc, lampGo, lampNg); Out(doc, lampNg, fork);
            L(doc, fork, "Branch1", aRun);
            L(doc, fork, "Branch2", bRun);
            Out(doc, aRun, aIn); Out(doc, aIn, aStop); Out(doc, aStop, aBlock); Out(doc, aBlock, join);
            Out(doc, bRun, bFree); Out(doc, bFree, bStop); Out(doc, bStop, join);

            Out(doc, join, shotHas); Out(doc, shotHas, opHas); Out(doc, opHas, jHas);
            L(doc, jHas, "条件1", shotPick);
            Out(doc, shotPick, opFind); Out(doc, opFind, jFind);
            L(doc, jFind, "条件1", opMove);
            Out(doc, opMove, jRange);
            L(doc, jRange, "条件1", mvX);
            Out(doc, mvX, mvY); Out(doc, mvY, down); Out(doc, down, settle); Out(doc, settle, vacOn);
            Out(doc, vacOn, suck); Out(doc, suck, up); Out(doc, up, unBlock); Out(doc, unBlock, aNext);
            Out(doc, aNext, toPut); Out(doc, toPut, shotPut); Out(doc, shotPut, opPutFind);
            Out(doc, opPutFind, opPutChk); Out(doc, opPutChk, jPut);
            L(doc, jPut, "条件1", readyOn);
            Out(doc, readyOn, sigReady); Out(doc, sigReady, ack); Out(doc, ack, jAck);
            L(doc, jAck, "条件1", blowOn);
            Out(doc, blowOn, vacOff); Out(doc, vacOff, drop); Out(doc, drop, blowOff);
            Out(doc, blowOff, readyOff); Out(doc, readyOff, picked); Out(doc, picked, placed);
            Out(doc, placed, shotEnd); Out(doc, shotEnd, opEnd); Out(doc, opEnd, jEnd);
            L(doc, jEnd, "条件1", bRun2);
            Out(doc, bRun2, bOut); Out(doc, bOut, bStop2); Out(doc, bStop2, cnt); Out(doc, cnt, end);
            return doc;
        }

        /// <summary>
        /// 不合格处理链：把判定节点的「否则」出口接到这条链的第一格，链内依次串起来（最后一格是结束节点）。
        /// 链上没有取料 / 放料 / 走位动作 —— 超差、缺料、对方没应答都只报警 + 记 NG + 叫停。
        /// </summary>
        static void Bad(NgDoc doc, NgNode gate, params NgNode[] chain)
        {
            doc.Nodes.AddRange(chain);
            L(doc, gate, "否则", chain[0]);
            for (int i = 0; i < chain.Length - 1; i++) Out(doc, chain[i], chain[i + 1]);
        }

        public static readonly NgFlowTemplate[] All =
        {
            new NgFlowTemplate
            {
                Key = "align",
                Name = "对位纠偏一条线（来料 → 拍 → 找Mark → 算偏差 → 判定 → 纠偏 → 复核 → 贴合）",
                Help = "生产循环的骨架。视觉那几步：找矩形四角 → 两个点对位（前缀「对位」）→ 算对位移动量（前缀「纠偏」）。"
                     + "判定拿的是变量「对位是否合格」：等于 1 才走 X/Y/R 三轴纠偏（走位的目标位置就写成 ${纠偏目标X} 这样），"
                     + "停稳后再拍一张、用「照基准比」复核一次，然后气缸贴合、回待机位；不合格那支只开报警输出、不贴合。"
                     + "铺完必须自己改这几格：等输入的信号名、三轴走位的轴名、算对位移动量里的标准位置和吸嘴偏移、贴合的气缸与回位的点位。"
                     + "对位的标准点第一次不知道填多少：先拿标准件跑一次，到视觉流程页点【记为标准】。",
                Build = AlignLine,
            },
            new NgFlowTemplate
            {
                Key = "cell",
                Name = "双皮带 + 4 相机 + 机构取料 + 机械手信号交互（机械手怎么动我们不管）",
                Help = "一条动作很简单、但互锁一个不能少的整线：皮带上送料，取放由机构（吸嘴 + X/Y 轴）做，"
                     + "机械手归它自己的控制器管，两边只交换逻辑信号。"
                     + "开工先过两道门：等「安全回路」接通、用【等对方发信号】收一条机械心跳（HEARTBEAT）确认对方在线；"
                     + "然后并行开两条皮带 —— 左皮带把料送到取料位（到位后停皮带 + 左挡停顶起），右皮带腾出放料位。"
                     + "接着四台相机各管一件事：①「来料相机」用按面积筛斑点看有没有料、正反对不对（前缀来料）；"
                     + "②「取料定位相机」照样子找图 + 算对位移动量（前缀取料纠偏），X/Y 走位的目标位置直接写 ${取料纠偏目标X}、${取料纠偏目标Y}，"
                     + "吸嘴降 → 真空吸 → 等 250 毫秒吸稳 → 升 → 挡停放开 → 左皮带送下一件；"
                     + "③「机械手取料位相机」用照样子找图 + 照基准比确认这件料真到位了（前缀放料确认），"
                     + "确认合格才把「视觉就绪」置 1 并发一条 V_READY，再用【等对方发信号】等 V_ACK（5 秒没等到就保持真空不放料、报「V_STOP」叫停）；"
                     + "④ 破真空（先吹气再关真空）→ 把「视觉就绪」放掉 → 等「机械手取料完成」「机械手放料完成」两个信号 → "
                     + "「成品复核相机」再数一次（前缀复核）→ 合格才开右皮带放行、产量+1。"
                     + "任何一道门不合格：只开报警灯、NG计数+1、需要时发 V_NOGOOD / V_HOLD / V_STOP / V_NG 让机械手别动，绝不带着没确认的信号继续取放。"
                     + "铺完要改的：四条 IO 输出名（皮带左跑 / 皮带右跑 / 真空吸 / 吹气 / 视觉就绪 / 绿灯 / 报警灯）、"
                     + "各路到位光电与两个等待输入的机械手信号名、挡停与吸嘴两个气缸名、X/Y 两根轴名与「取放工位 / 机械手取料位」点位、"
                     + "四台相机名（要和「相机」页一致）、两条通道（「通讯」页里那条，或直接填 主机:端口）、"
                     + "两处「照样子找图」的样例图文件、以及「算对位移动量」里的标准位置和吸嘴离相机的偏移；"
                     + "第一次上机：每台相机先各跑一次「多点标定」并存进「标定」表，「照基准比」的基准拿标准件跑一次后点【记为标准】。",
                Build = CellLine,
            },
        };

        public static NgFlowTemplate Find(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            foreach (var t in All) if (t.Key == key.Trim()) return t;
            return null;
        }
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░�▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥
