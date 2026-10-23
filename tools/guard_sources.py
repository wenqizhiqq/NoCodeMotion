# -*- coding: utf-8 -*-
r"""源码文本回归守卫 —— 伺服使能极性 / Jog 失败提示 / 加减速语义 / 暂停停止守卫 / 多开退出 / 图形生成 / IO 虚拟化。

为什么单独一个脚本：C# 冒烟程序从 %TEMP% 启动，读 D:\ 下的源码会被沙箱替换成无效内容
（实测读到 237036 字节乱码，真实文件 232940 字节）。本脚本从仓库目录运行，读到的才是真文件。

★ 2026-10-07：本脚本原在 %TEMP%\ncm_patch\，被系统临时目录清理删掉过一次 →
  改为放仓库内 tools\（持久）。用法（必须在仓库根目录跑）：
    C:/Users/admin/.workbuddy-ai/binaries/python/versions/3.13.12/python.exe -u tools/guard_sources.py
"""
import os
import re
import sys

ROOT = os.getcwd()

SDK = r"Services\Hardware\Cards\Families\MCN42Series\MCN420SeriesSDK.cs"
AXIS = r"Services\Hardware\Cards\Families\MCN42Series\AxisRealization.cs"
CAT = r"Services\Hardware\Cards\CardFamilyCatalog.cs"
BRIDGE = r"Services\Hardware\Cards\WenQiZhiCardBridge.cs"

npass = nfail = 0
fails = []


def check(name, ok, detail=""):
    global npass, nfail
    if ok:
        npass += 1
        print(f"  PASS  {name}")
    else:
        nfail += 1
        line = f"  FAIL  {name}" + (f"  ::  {detail}" if detail else "")
        fails.append(line)
        print(line)


def read(rel):
    p = os.path.join(ROOT, rel)
    if not os.path.exists(p):
        return None
    # 逐文件判断 BOM / 行尾，不做任何转换
    raw = open(p, "rb").read()
    return raw.decode("utf-8-sig")


def section(t):
    print(f"\n===== {t} =====")


# ---------------------------------------------------------------------
section("G1  研控 MCN420：使能写 0、不使能写 1（端口电平低有效；真机实测，防回归）")
# ---------------------------------------------------------------------
sdk = read(SDK)
if sdk is None:
    check("G1.0 找到 MCN420SeriesSDK.cs", False, SDK)
else:
    check("G1.0 找到 MCN420SeriesSDK.cs", True)
    check("G1.1 Enable 写 0：YK_set_sevon_config(..., 0)（低电平有效＝使能）",
          sdk.count("return MCN420.YK_set_sevon_config((ushort)CardNo, (ushort)axis, 0);") == 1,
          "count=%d" % sdk.count("return MCN420.YK_set_sevon_config((ushort)CardNo, (ushort)axis, 0);"))
    check("G1.2 Disable 写 1：YK_set_sevon_config(..., 1)",
          sdk.count("return MCN420.YK_set_sevon_config((ushort)CardNo, (ushort)axis, 1);") == 1,
          "count=%d" % sdk.count("return MCN420.YK_set_sevon_config((ushort)CardNo, (ushort)axis, 1);"))
    # 反向断言：不能再出现「Enable 写 1」这种被改反的写法（2026-09-30 二次修正的错法）
    check("G1.3 没有把 Enable 写成 1（极性反了的特征）",
          "NmcSetCardAxisEnable" in sdk
          and sdk.split("public int NmcSetCardAxisEnable")[1].split("public int NmcSetCardAxisDisable")[0]
              .count("YK_set_sevon_config((ushort)CardNo, (ushort)axis, 1)") == 0,
          "Enable 函数体里出现了写 1")
    check("G1.4 记录真机实测结论（三次修正，写 0）",
          "三次修正" in sdk and "低电平有效" in sdk)
    check("G1.5 不再宣称「逻辑使能：1 = 使能、0 = 不使能」",
          "逻辑使能**，不是端口电平" not in sdk)
    check("G1.6 错误码译码抗 XML 缺失（FormatErrorCode）",
          "private string FormatErrorCode(int ErrNum, string label)" in sdk)
    check("G1.7 错误码表惰性加载（EnsureErrorCodeItems）",
          "private List<ErrorCode> EnsureErrorCodeItems()" in sdk)
    check("G1.8 探测多个部署路径（BaseDirectory / Native / Cards）",
          'Path.Combine(dir, "Native", xmlPath)' in sdk and 'Path.Combine(dir, "Cards", xmlPath)' in sdk)
    check("G1.9 错误码文本折成单行（NormalizeCodeText）",
          "private static string NormalizeCodeText(string s)" in sdk
          and "{label}：{NormalizeCodeText(it.ZhCn)}" in sdk)

# ---------------------------------------------------------------------
section("G2  状态字 bit8 与读回归一化")
# ---------------------------------------------------------------------
axis = read(AXIS)
if axis is None:
    check("G2.0 找到 MCN42Series/AxisRealization.cs", False, AXIS)
else:
    check("G2.0 找到 MCN42Series/AxisRealization.cs", True)
    check("G2.1 状态字 bit8 以读回 == 0 判定使能（低电平有效）",
          "YK_get_sevon_config((uint)card, (uint)axis) == 0" in axis
          and "YK_get_sevon_config((uint)card, (uint)axis) == 1" not in axis)
    check("G2.2 NormalizeSevon 是反转映射（0→1 已使能、1→0 不使能），不是恒等",
          "if (rawSevon == 0) return 1;" in axis and "if (rawSevon == 1) return 0;" in axis)
    check("G2.3 卡族层不再说「研控 MCN420 的 Sevon 不是低电平有效」",
          "研控 MCN420 的 Sevon <b>不是</b>低电平有效" not in axis)
    check("G2.4 卡族层明确低电平有效（已使能时读回 0）",
          "低电平有效" in axis)
    check("G2.5 CardAxisWriteSevonPin：on_off==0 → Disable",
          "if (on_off == 0)" in axis
          and "NmcSetCardAxisDisable(AxisWhichCardNo, AxisID)" in axis)
    check("G2.6 GetCardAxisSevonPin 文档已改为 CiA402 状态机（不再是端口电平）",
          "读取指定轴的 CiA402 状态机值" in axis)

# ---------------------------------------------------------------------
section("G3  卡族登记表：Sevon 说明与 8194/20480 归因")
# ---------------------------------------------------------------------
cat = read(CAT)
if cat is None:
    check("G3.0 找到 CardFamilyCatalog.cs", False, CAT)
else:
    check("G3.0 找到 CardFamilyCatalog.cs", True)
    check("G3.1 不说「逻辑使能：写 1 = 使能、写 0 = 不使能」（2026-09-30 二次修正的错法）",
          "★ Sevon 的第三参是「逻辑使能」：写 1 = 使能、写 0 = 不使能。" not in cat)
    check("G3.2 改为「端口电平：低电平有效 → 写 0 = 使能、写 1 = 不使能」",
          "端口电平" in cat and "写 0 = 使能、写 1 = 不使能" in cat)
    check("G3.3 不再把 8194 / 20480 归因于脉冲当量",
          "当量为 0 或被卡拒绝时会返回 8194 / 20480" not in cat)
    check("G3.4 指出方向与雷赛/模拟卡 dmc_write_sevon_pin 一致（都低电平有效）",
          "dmc_write_sevon_pin（同样低电平有效，0 = 使能）一致" in cat)

# ---------------------------------------------------------------------
section("G4  Jog 失败提示：译码 + 当量下发结果")
# ---------------------------------------------------------------------
bridge = read(BRIDGE)
if bridge is None:
    check("G4.0 找到 WenQiZhiCardBridge.cs", False, BRIDGE)
else:
    check("G4.0 找到 WenQiZhiCardBridge.cs", True)
    check("G4.1 AxisFailHint 签名带 (AxisItem, IAxis, int)",
          "private static string AxisFailHint(AxisItem axis, IAxis a, int code)" in bridge)
    check("G4.2 三个调用点都传了 (axis, a, res)",
          bridge.count("AxisFailHint(axis, a, res)") == 3,
          "count=%d" % bridge.count("AxisFailHint(axis, a, res)"))
    check("G4.3 不再有单参调用 AxisFailHint(axis)",
          "AxisFailHint(axis)" not in bridge)
    check("G4.4 有卡返回码译码（CardCodeText + 反射缓存）",
          "private static string CardCodeText(IAxis a, int code)" in bridge
          and "_errInfoMethods" in bridge)
    check("G4.5 记录当量下发结果（_lastRatioRes）",
          "private static readonly ConcurrentDictionary<string, int> _lastRatioRes" in bridge)
    check("G4.6 EnsureAxisRatio 里写入下发结果",
          "_lastRatioRes[RatioKey(axis)] = res;" in bridge)
    check("G4.7 异常路径也记录（RatioPushThrew）",
          "_lastRatioRes[RatioKey(axis)] = RatioPushThrew;" in bridge)
    check("G4.8 提示里读当量结果并分三种文案",
          "bool hasRatio = _lastRatioRes.TryGetValue(RatioKey(axis), out rres);" in bridge
          and "未成功写入卡" in bridge
          and "已下发成功" in bridge
          and "抛异常" in bridge)
    check("G4.9 不再把 8194/20480 归因于脉冲当量（提示正文）",
          "为 0 或填错都会让卡拒绝" not in bridge)
    check("G4.10 注释里点明 8194 = 普通缓冲满、20480 = 0通道轴未运动完成",
          "8194 = 普通缓冲满出错、20480 = 0 通道轴未运动完成" in bridge)
    check("G4.11 EquivOf 当量 <=0 时兜底为 1（不会乘 0 / 除 0）",
          "axis.PulsePerUnit > 0 ? axis.PulsePerUnit : 1" in bridge)

# ---------------------------------------------------------------------
section("G5  部署资源：Mcn420ErrorCode.xml")
# ---------------------------------------------------------------------
xml_rel = r"Native\Mcn420ErrorCode.xml"
xml = os.path.join(ROOT, xml_rel)
check("G5.1 Native/Mcn420ErrorCode.xml 存在", os.path.exists(xml))
if os.path.exists(xml):
    b = open(xml, "rb").read()
    check("G5.2 内容非空（>5KB）", len(b) > 5000, "len=%d" % len(b))
    check("G5.3 根元素为 <errorcodes>", b"<errorcodes>" in b)
    check("G5.4 含 code=8194 且为「普通缓冲满出错」",
          'code="8194"' in b.decode("utf-8-sig") and "普通缓冲满出错" in b.decode("utf-8-sig"))
csproj = read("NoCodeMotion.csproj")
check("G5.5 csproj 已声明随程序复制（输出落点 = exe 同级的 Mcn420ErrorCode.xml）",
      csproj is not None and 'TargetPath="Mcn420ErrorCode.xml"' in csproj
      and 'Mcn420ErrorCode.xml' in csproj)

# ---------------------------------------------------------------------
section("G6  缓冲占用类错误码：先给「先停再 Jog」的动作，不再反问轴号/当量")
# ---------------------------------------------------------------------
if bridge is not None:
    check("G6.1 有 IsBufferBusyCode 判定函数",
          "private static bool IsBufferBusyCode(int code)" in bridge)
    # 10 个缓冲占用类码字都要在 switch 里
    buf_codes = ["8192", "8193", "8194", "12288", "12289", "20480", "20481", "24576", "24577", "60000"]
    missing = [c for c in buf_codes if f"case {c}:" not in bridge]
    check("G6.2 switch 覆盖 10 个缓冲占用类码字",
          not missing, "缺: %s" % ",".join(missing))
    # ★ 提示文案在「Jog 前先自动停」引入后改写：现在首条动作是①手动点停+Jog，不再是
    #   「请先到轴页点「停止」清空卡内缓冲」那种单一说法。断言改为匹配新文案的骨架。
    _busy_branch = bridge.split("if (bufferBusy)")[1].split("sb.Append(\"请依次确认：\")")[0]
    check("G6.3 AxisFailHint 里取 bufferBusy 并提前返回",
          "bool bufferBusy = IsBufferBusyCode(code);" in bridge
          and "if (bufferBusy)" in bridge
          and "return sb.ToString();" in _busy_branch)
    check("G6.4 缓冲满分支先说明「卡内已有动作占着缓冲」并给出按序动作",
          "该码说明卡内已有一条动作占着缓冲、还没执行完，新指令进不去。" in _busy_branch
          and "① 到轴页点「停止」再点「Jog」" in _busy_branch
          and "③ 再不行就断电重启控制卡" in _busy_branch
          and "return sb.ToString();" in _busy_branch)
    check("G6.5 当量提示被限定为「非缓冲满」时才报主因",
          "if (!bufferBusy && hasRatio)" in bridge)
    check("G6.6 缓冲满分支不再列「脉冲当量」这条已被排除的项",
          "脉冲当量" not in _busy_branch
          and "该码说明卡内已有一条动作占着缓冲" in _busy_branch)
    check("G6.7 通用 5 项清单保留（非缓冲满码仍走全表）",
          bridge.count("请依次确认：") == 1
          and "名称里的数就是轴号：轴0、轴1、轴2" in bridge)

# ---------------------------------------------------------------------
section("G7  轴/IO 名称从 0 开始、且名称 = 轴号（轴0、输入0、输出0）")
# ---------------------------------------------------------------------
if bridge is not None:
    check("G7.1 有 _warnedAxisDuplicate 去重集合",
          "private readonly HashSet<string> _warnedAxisDuplicate" in bridge)
    check("G7.2 AxisOf 命中已占用轴号时按轴名判重并告警",
          "if (!string.Equals(existing.AxisName, axis.Name, StringComparison.Ordinal))" in bridge
          and "[卡族·警告] 控制器「{axis.Controller}」上，轴「{axis.Name}」与轴「{existing.AxisName}」" in bridge)
    check("G7.3 告警说明「两根轴会共用同一个底层轴对象」",
          "两根轴会共用同一个底层轴对象" in bridge)
    check("G7.4 告警点出与 IO 序号一样从 0 开始且不可重复",
          "**从 0 开始且不可重复**" in bridge and "名称里的数就是轴号：轴0、轴1、轴2" in bridge)
    check("G7.5 通用清单第 ② 项改为讲清「轴号从 0 开始、不可重复」的映射",
          "该轴「轴号」与卡上轴号是否一致 —— 注意**「轴号」和 IO 序号一样从 0 开始、且不可重复**" in bridge
          and "名称里的数就是轴号：轴0、轴1、轴2" in bridge)
    check("G7.6 提示点出「两根轴填同一个轴号会共用底层通道、Jog 报 8194」",
          "两根轴填了同一个轴号，它们会共用同一个底层轴通道" in bridge)

# 自动生成器（AxisControllerViewModel）必须 0 基，且名称 = 轴号
avm = read(r"ViewModels\AxisControllerViewModel.cs")
if avm is None:
    check("G7.7 找到 AxisControllerViewModel.cs", False)
else:
    check("G7.7 找到 AxisControllerViewModel.cs", True)
    # ★ 2026-10-19 起：连接控制器改成「只按本卡调数量、不改名称与参数」，
    #   生成器不再是 for s=0..count 的整体重建，而是「找未占用的最小轴号」补位（仍 0 基）。
    check("G7.8 轴自动生成：名称「轴<轴号>」= 轴号（MakeAxis(ctl, name, no)）",
          "data.Axes.Add(MakeAxis(ctl, name, no));" in avm
          and 'string name = $"轴{no}";' in avm)
    check("G7.8b 轴号从 0 起找未占用的最小号（int no = 0; while (...AxisNo == no) no++;）",
          "int no = 0;" in avm
          and "while (data.Axes.Any(a => a != null && a.Controller == tag && a.AxisNo == no)) no++;" in avm)
    check("G7.9 IO 自动生成：主板位号从 0 起（inSlots.Add((0, s))）",
          "for (int s = 0; s < System.Math.Max(ctl.InIoCount, 0); s++) inSlots.Add((0, s));" in avm
          and 'AdjustIoSlots(data.Inputs, tag, inSlots, "输入");' in avm)
    check("G7.10 不再有 1 基的 MakeAxis(tag, s, s - 1)",
          "MakeAxis(tag, s, s - 1)" not in avm)

# 名称 —— 各处占位名/新增名也必须 0 基
for rel, tok, name in [
    (r"ViewModels\AxisViewModel.cs", 'Name = $"轴{Counter}"', "新增轴名 0 基"),
    (r"ViewModels\EngineerViewModel.cs", '"轴{i}"', "Jog 提示占位名 0 基"),
]:
    t = read(rel)
    check(f"G7.11 {name}（{rel}）", t is not None and tok in t,
          "" if t is None else ("缺: " + tok))
pt = read(r"ViewModels\PointViewModel.cs")
check("G7.12 PointViewModel 占位名 0 基（CSV 表头 + Jog 提示）",
      pt is not None and pt.count('"轴{i}"') >= 2)
check("G7.13 全仓不再有 1 基的轴占位名（轴{i + 1} / 轴{Counter + 1}）",
      avm is not None and "轴{i + 1}" not in avm
      and (read(r"ViewModels\AxisViewModel.cs") or "").count("轴{Counter + 1}") == 0
      and (read(r"ViewModels\EngineerViewModel.cs") or "").count("轴{i + 1}") == 0
      and (pt or "").count("轴{i + 1}") == 0)


# ---------------------------------------------------------------------
section("G8  Jog 防 8194：启动前先停轴清缓冲 + 缓冲占用诊断 + 原点电平取对字段")
# ---------------------------------------------------------------------
g8_bridge = read(BRIDGE)
if g8_bridge is None:
    check("G8.0 找到 WenQiZhiCardBridge.cs", False, BRIDGE)
else:
    check("G8.0 找到 WenQiZhiCardBridge.cs", True)
    # Jog 前先停一次轴（立刻停）清缓冲
    jidx = g8_bridge.find("public string StartAxisJog")
    jseg = g8_bridge[jidx:jidx + 3000] if jidx >= 0 else ""
    check("G8.1 Jog 前调 StopCardAxisMovement 停一次轴（清卡内缓冲）",
          "StopCardAxisMovement(cardNo, Math.Max(axis.AxisNo, 0), 1)" in jseg,
          "在 StartAxisJog 里没找到启动前停轴")
    check("G8.2 用的是「立刻停」(stop_mode=1)，不是减速停",
          "StopCardAxisMovement(cardNo, Math.Max(axis.AxisNo, 0), 1)" in jseg
          and "StopCardAxisMovement(cardNo, Math.Max(axis.AxisNo, 0), 0)" not in jseg)
    # 缓冲占用诊断
    check("G8.3 有 GetCmdListBufNum 反射句柄缓存 _bufNumMethods",
          "_bufNumMethods" in g8_bridge and "ConcurrentDictionary<Type, System.Reflection.MethodInfo>" in g8_bridge)
    check("G8.4 有 ClearBufferHint 诊断方法（写「指令缓冲占用」日志）",
          "private void ClearBufferHint(" in g8_bridge and "指令缓冲占用" in g8_bridge)
    check("G8.5 诊断只读不抛（try/catch 包住，不影响 Jog）",
          "catch { /* 诊断失败就算了" in g8_bridge)
    check("G8.6 Jog 里调用了 ClearBufferHint",
          "ClearBufferHint(slot, a, axis)" in jseg)
    # 原点电平字段修对
    check("G8.7 BuildHomeParam 的 OrgLevel 取自 OriginLevel（不再误用 EnableLevel）",
          "OrgLevel = (axis.OriginLevel" in g8_bridge
          and "OrgLevel = (axis.EnableLevel" not in g8_bridge,
          "OrgLevel 仍读 EnableLevel")
    # 8194 提示改为「先点停止再 Jog → 查上一条为何不结束 → 断电」
    check("G8.8 8194 提示给出「先点停止再 Jog」的自愈动作",
          "到轴页点「停止」再点「Jog」" in g8_bridge)
    check("G8.9 8194 提示讲解「上一条运动为何结束不了」",
          "上一条运动为什么结束不了" in g8_bridge)
    check("G8.10 8194 提示最后给「断电重启控制卡」的兜底",
          "断电重启控制卡" in g8_bridge)

# 研控 SDK / 轴实现新增 GetCmdListBufNum
g8_sdk2 = read(SDK)
check("G8.11 SDK 新增 GetCmdListBufNum（包装 YK_get_cmd_list_buf_num）",
      g8_sdk2 is not None and "public int GetCmdListBufNum(int CardNo, int channel = 0)" in g8_sdk2
      and "YK_get_cmd_list_buf_num" in g8_sdk2)
g8_ax2 = read(AXIS)
check("G8.12 轴实现新增 GetCmdListBufNum（桥接层反射的目标）",
      g8_ax2 is not None and "public int GetCmdListBufNum(int CardNo)" in g8_ax2)

# ---------------------------------------------------------------------
section("G9  MCN420 Channel 字段必须是 0/1（曾误写 2 → YK_vmove 回 8194 普通缓冲满）")
# ---------------------------------------------------------------------
# 原生 struct 注释与 YK_vmove 文档都写「通道 0 or 1」。写 2 会被卡拒绝。
# 这条守卫的作用：任何一处 Channel=2 复活都立刻变红（含未来新加的请求结构体）。
g9_sdk = read(SDK)
g9_native = read(r"Services\Hardware\Cards\Native\MCN420.cs")
if g9_sdk is None or g9_native is None:
    check("G9.0 找到 MCN420SeriesSDK.cs / Native/MCN420.cs", False)
else:
    check("G9.0 找到 MCN420SeriesSDK.cs / Native/MCN420.cs", True)

    # 只统计「赋值语句」，忽略注释里的 Channel = 2（如被注释掉的 YK_set_output_delay_flip）
    bad = []
    for ln, line in enumerate(g9_sdk.split("\n"), 1):
        s = line.strip()
        if s.startswith("//"):
            continue
        if "Channel = 2" in s or "Channel=2" in s:
            bad.append(f"{ln}: {s[:60]}")
    check("G9.1 全文件没有活的 `Channel = 2` 赋值（注释除外）",
          not bad, "；".join(bad))

    # 四处曾经出错的落点都要是 Channel = 0
    check("G9.2 OpenCard 预置 Jog_Req_Param 的 Channel = 0",
          "AxisVMoveParaDic.TryAdd(Tuple.Create(i, j), new Jog_Req_Param()" in g9_sdk)
    n0 = g9_sdk.count("Channel = 0,")
    check("G9.3 `Channel = 0` 至少 4 处（Jog 预置 / PMove 预置 / ProfileUnit / IOCountMode）",
          n0 >= 4, f"实际 {n0} 处")
    check("G9.4 DmcSetCardAxisProfileUnit 重建的 Jog_Req_Param 里是 Channel = 0（Jog 实际走这条）",
          "Channel = 0," in g9_sdk.split(
              "AxisVMoveParaDic[Tuple.Create(CardNo, axis)] = new Jog_Req_Param\r\n                {")[1][:500])

    # 原生声明侧：struct 注释必须仍写「0 or 1」——这是判断值的唯一权威依据
    check("G9.5 Jog_Req_Param.Channel 原生注释仍是「0 or 1」",
          "public uint Channel;                                     // 0 or 1" in g9_native)
    check("G9.6 YK_vmove 文档仍写「通道0 or 1」",
          "Channel：通道0 or 1" in g9_native
          or "通道0 or 1" in g9_native)
    check("G9.7 MaxInterpChannel=2 是「插补通道数」常量、与 Channel 字段无关（零引用）",
          "public const int MaxInterpChannel = 2;" in g9_native)

# ---------------------------------------------------------------------
section("G10  桥接层反射/停轴细节（原在 %TEMP% 冒烟里，因沙箱截断源码而假 FAIL，挪到这里）")
# ---------------------------------------------------------------------
# 说明：%TEMP% 启动的冒烟进程用 File.ReadAllText 读 D:\ 源码会被沙箱替换/截断
# （实测 WenQiZhiCardBridge.cs 真 103866 字节，只读到 102745 字符）→ Contains 假 FAIL。
# 凡「读源码文本」的断言都必须在仓库根目录跑，也就是这里。
g10 = read(BRIDGE)
if g10 is None:
    check("G10.0 找到 WenQiZhiCardBridge.cs", False, BRIDGE)
else:
    check("G10.0 找到 WenQiZhiCardBridge.cs", True)
    check("G10.1 反射 GetCmdListBufNum 时带 new[]{typeof(int)} 参数类型（否则重载歧义）",
          "null, new[] { typeof(int) }, null)" in g10)
    check("G10.2 StartAxisJog 里 Jog 前停轴用的是 stop_mode=1（立刻停）",
          "a.StopCardAxisMovement(cardNo, Math.Max(axis.AxisNo, 0), 1)" in g10)
    check("G10.3 ClearBufferHint 同时推状态栏（手动 Jog 时 Log 看不见）",
          "StatusBarService.ReportInfo" in g10
          and "Jog 前缓冲读数" in g10)

# ---------------------------------------------------------------------
section("G11  轴默认速度 / 默认寸动 = 10000，且老工程载入时写回（含幂等）")
# ---------------------------------------------------------------------
g11_axis = read(r"Models\AxisItem.cs")
g11_data = read(r"Models\ProjectData.cs")
if g11_axis is None or g11_data is None:
    check("G11.0 找到 AxisItem.cs / ProjectData.cs", False)
else:
    check("G11.0 找到 AxisItem.cs / ProjectData.cs", True)
    check("G11.1 AxisItem._speed 默认 10000（运行速度）",
          "_speed = 10000" in g11_axis and "_speed = 100;" not in g11_axis)
    check("G11.2 AxisItem._jogStep 默认 10000（点动距离/寸动）",
          "_jogStep = 10000" in g11_axis and "_jogStep = 1;" not in g11_axis)
    check("G11.3 AxisItem._manualSpeed 默认 10000（手动速度）",
          "_manualSpeed = 10000" in g11_axis and "_manualSpeed = 20;" not in g11_axis)

    check("G11.4 ProjectData 有 MigrateAxisDefaults()",
          "public void MigrateAxisDefaults()" in g11_data)
    check("G11.5 EnsurePointTables 末尾调用了迁移（两条载入路径都必经它）",
          "MigrateAxisDefaults();" in g11_data)
    check("G11.6 迁移定义了新旧三组常量",
          "LegacyAxisSpeed = 100" in g11_data
          and "LegacyAxisJogStep = 1" in g11_data
          and "LegacyAxisManualSpeed = 20" in g11_data
          and "DefaultAxisSpeed = 10000" in g11_data
          and "DefaultAxisJogStep = 10000" in g11_data
          and "DefaultAxisManualSpeed = 10000" in g11_data)
    # ★ 只改「还等于旧默认值」的轴 —— 用户亲手调过的值不能被覆盖
    check("G11.7 迁移只在等于旧默认值时才改（不覆盖用户设过的值）",
          "if (a.Speed == LegacyAxisSpeed) a.Speed = DefaultAxisSpeed;" in g11_data
          and "if (a.JogStep == LegacyAxisJogStep) a.JogStep = DefaultAxisJogStep;" in g11_data
          and "if (a.ManualSpeed == LegacyAxisManualSpeed) a.ManualSpeed = DefaultAxisManualSpeed;" in g11_data)
    check("G11.8 每一步都有 `if (a == null) continue;` 防空引用",
          "if (a == null) continue;" in g11_data)
    # ★ 新增：加减速语义由「加速度值」改为「加速时间(秒)」后，老工程的 Accel/Decel=50 也须迁移
    check("G11.9 AxisItem._accel/_decel 默认 0.2 秒（加速时间，不再是 50）",
          "_accel = 0.2;" in g11_axis and "_decel = 0.2;" in g11_axis
          and "_accel = 50;" not in g11_axis and "_decel = 50;" not in g11_axis)
    check("G11.10 迁移含 Accel/Decel：LegacyAxisAccel=50 -> DefaultAxisAccel=0.2，且只在等于旧值时改",
          "LegacyAxisAccel = 50" in g11_data
          and "DefaultAxisAccel = 0.2" in g11_data
          and "if (a.Accel == LegacyAxisAccel) a.Accel = DefaultAxisAccel;" in g11_data
          and "if (a.Decel == LegacyAxisAccel) a.Decel = DefaultAxisAccel;" in g11_data)

# ---------------------------------------------------------------------
section("G12  加减速语义 = 加速时间(秒)，且速度带单位换算（Jog 日志写明 unit/s 与脉冲当量）")
# ---------------------------------------------------------------------
g12_bridge = read(r"Services\Hardware\Cards\WenQiZhiCardBridge.cs")
g12_ltd = read(r"Services\Hardware\Leadshine\LtdmcCard.cs")
if g12_bridge is None or g12_ltd is None:
    check("G12.0 找到 WenQiZhiCardBridge.cs / LtdmcCard.cs", False)
else:
    check("G12.0 找到 WenQiZhiCardBridge.cs / LtdmcCard.cs", True)
    # 桥接层：Accel/Decel 直接当秒用，不再 v/accel
    check("G12.1 BuildMotionParam：tacc = Accel 原值（秒），不再 v / axis.Accel",
          "double tacc = axis.Accel > 0 ? axis.Accel : 0.2;" in g12_bridge
          and "v / axis.Accel" not in g12_bridge)
    check("G12.2 BuildMotionParam：tdec = Decel 原值（秒），兜底取 tacc",
          "double tdec = axis.Decel > 0 ? axis.Decel : tacc;" in g12_bridge
          and "v / axis.Decel" not in g12_bridge)
    check("G12.3 BuildHomeParam：TaccTime/TdecTime 也按秒（不再 high / Accel）",
          "TaccTime = axis.Accel > 0 ? axis.Accel : 0.2," in g12_bridge
          and "TdecTime = axis.Decel > 0 ? axis.Decel : 0.2," in g12_bridge
          and "high / axis.Accel" not in g12_bridge)
    check("G12.4 注释记录了「400 秒斜坡 → 轴爬行」的真因（防止被改回去）",
          "400 秒" in g12_bridge or "400s" in g12_bridge)
    # 单位换算可见性：Jog 日志同时给出 unit/s 与脉冲当量
    check("G12.5 Jog 日志写明「unit/s + 脉冲当量」（模拟卡写明 ×当量=pps）",
          "unitNote" in g12_bridge
          and "脉冲当量" in g12_bridge
          and "pulse/s" in g12_bridge)
    check("G12.6 Jog 日志带出加减速秒数，便于现场核对",
          "axis.Accel:0.###" in g12_bridge and "加减速时间" in g12_bridge)
    # ★ 自诊断：残留旧「加速度值」大数（如 50）会被读成 50 秒斜坡 -> 主动告警
    check("G12.6b Jog 时对偏大的加减速时间主动告警（Accel>=5s 提示可能是旧加速度值）",
          "axis.Accel >= 5" in g12_bridge
          and "不是加速度值" in g12_bridge
          and "rampWarn" in g12_bridge)
    # 雷赛层同口径
    check("G12.7 LtdmcCard.ApplyAxisProfile：tacc = accel 原值（秒）",
          "double tacc = accel > 0 ? accel : 0.2;" in g12_ltd
          and "maxVel / accel" not in g12_ltd)
    check("G12.8 LtdmcCard.SetSpeed：tacc = accel 原值（秒）",
          g12_ltd.count("double tacc = accel > 0 ? accel : 0.2;") >= 2)
    check("G12.9 LtdmcCard.Home：tacc = accel 原值（秒），不再 high / accel",
          "high / accel" not in g12_ltd)
    # 卡规范侧：InterpoAcc 仍是 unit/ms²（卡层按 (vmax-vmin)/tacc*1000 换算，语义不变）
    g12_sdk = read(r"Services\Hardware\Cards\Families\MCN42Series\MCN420SeriesSDK.cs")
    check("G12.10 卡层仍按 (Max_Vel-Min_Vel)/(Tacc*1000) 把秒换成 unit/ms²（Tacc 语义就是秒）",
          g12_sdk is not None
          and "InterpoAcc = ((Max_Vel / 1000) - (Min_Vel / 1000)) / (Tacc * 1000)," in g12_sdk)
    # UI 标签与语义一致（旧标签「加速度 / 减速度」会被误解成值）
    g12_xaml = read(r"Views\AxisPage.xaml")
    check("G12.11 轴页标签改为「加速时间 / 减速时间」（与秒语义一致），不再叫「加速度 / 减速度」",
          g12_xaml is not None
          and 'Text="加速时间"' in g12_xaml
          and 'Text="减速时间"' in g12_xaml
          and 'Text="加速度"' not in g12_xaml
          and 'Text="减速度"' not in g12_xaml)
    check("G12.12 轴页两个字段带了说明 ToolTip（写明是时间、常用 0.1~0.5）",
          g12_xaml is not None
          and "ToolTip" in g12_xaml
          and "常用 0.1~0.5" in g12_xaml)

# ---------------------------------------------------------------------
section("G13  暂停/停止守卫：静态 WaitGuard + 反射绑定 + OperationCanceledException 上抛（Request B）")
# ---------------------------------------------------------------------
# 设计：进程级静态 HardwareBridge.WaitGuard 由所有阻塞等待（Delay / MoveAxis 等）每 ≤50ms 轮询。
#   Pause → ResumeEvent.Wait() 阻塞；Stop/EStop → 抛 OperationCanceledException（不是 ScriptRuntimeException，
#   否则被 ExecuteLeaf 的 catch(Exception) 吞掉、流程继续跑）。每次运行用 BindWaitGuard(ctrl)（反射，避环依赖）。
g13_hb = read(r"Services\HardwareBridge.cs")
g13_frs = read(r"ViewModels\FlowRunnerService.cs")
g13_lead = read(r"Services\Hardware\Leadshine\LeadshineHardwareBridge.cs")
if g13_hb is None or g13_frs is None or g13_lead is None:
    check("G13.0 找到 HardwareBridge.cs / FlowRunnerService.cs / LeadshineHardwareBridge.cs", False)
else:
    check("G13.0 找到关键文件", True)
    check("G13.1 静态闸：public static volatile Action WaitGuard",
          "public static volatile Action WaitGuard;" in g13_hb)
    check("G13.2 BindWaitGuard 用 object + 反射取字段（避 Services→ViewModels 环依赖）",
          "public static bool BindWaitGuard(object ctrl)" in g13_hb
          and 't.GetField("StopRequested")' in g13_hb
          and 't.GetField("PauseRequested")' in g13_hb
          and 't.GetField("ResumeEvent")' in g13_hb)
    check("G13.3 停止/急停抛 OperationCanceledException（且 Pause 走 ResumeEvent.WaitOne）",
          g13_hb.count("throw new OperationCanceledException") >= 3
          and "ev.WaitOne()" in g13_hb
          and 'fPause.GetValue(ctrl)' in g13_hb)
    check("G13.4 Hook/UnhookWaitGuard 在 RunAllAsync 起线程前绑定、watchdog 收尾解绑",
          "private static void HookWaitGuard(FlowRunControl ctrl)" in g13_frs
          and "private static void UnhookWaitGuard(FlowRunControl ctrl)" in g13_frs
          and "HardwareBridge.BindWaitGuard(ctrl);" in g13_frs
          and "HardwareBridge.BindWaitGuard(null);" in g13_frs)
    check("G13.5 ExecuteLeaf 尾 catch 把 OperationCanceledException / FlowAbortException 原样上抛（停止不被吞）",
          "if (ex is OperationCanceledException || ex is FlowAbortException) throw;" in g13_frs)
    check("G13.6 速度可改路径：ExecAxis prop==速度 → SetAxisSpeed；ExecPoint slot.Speed>0 → SetAxisSpeed",
          "if (double.TryParse(setv, out var spd) && spd > 0) _bridge?.SetAxisSpeed(ax, spd);" in g13_frs
          and "if (slot.Speed > 0) _bridge?.SetAxisSpeed(ax, slot.Speed);" in g13_frs)
    check("G13.7 雷赛 WaitInterruptible 读**进程级静态** HardwareBridge.WaitGuard（不是实例 WaitGuard）",
          "HardwareBridge.WaitGuard?.Invoke();" in g13_lead
          and "WaitGuardDetectStopOnly() => HardwareBridge.WaitGuard?.Invoke();" in g13_lead)
# 卡族（研控）层同样读静态闸
g13_wq = read(BRIDGE)
check("G13.8 研控卡族 WaitInterruptible 读静态 HardwareBridge.WaitGuard",
      g13_wq is not None and "HardwareBridge.WaitGuard?.Invoke();" in g13_wq)
check("G13.9 Lua 运行 catch：停止/急停按 Warn 记（不再当红色 Error 刷屏）",
      g13_frs is not None
      and '已{(ctrl.EStopRequested ? "急停" : "停止")}（等待被中断）。", LogLevel.Warn' in g13_frs)

# ---------------------------------------------------------------------
section("G14  多开保护 + 退出进度条释放资源（Request C）")
# ---------------------------------------------------------------------
g14_si = read(r"Services\SingleInstance.cs")
g14_ash = read(r"Services\AppShutdown.cs")
g14_app = read(r"App.xaml.cs")
g14_mw = read(r"MainWindow.xaml.cs")
g14_icw = read(r"Views\InstanceConflictWindow.xaml")
g14_cpw = read(r"Views\ClosingProgressWindow.xaml")
if g14_si is None or g14_ash is None or g14_app is None or g14_mw is None or g14_icw is None or g14_cpw is None:
    check("G14.0 找到 单实例/释放/入口/关闭/两个窗体 文件", False)
else:
    check("G14.0 找到 单实例/释放/入口/关闭/两个窗体 文件", True)
    check(r"G14.1 命名 Mutex 带用户名（Local\NoCodeMotion_SingleInstance_<user>）",
          'Local\\NoCodeMotion_SingleInstance_' in g14_si
          and "System.Environment.UserName" in g14_si)
    check("G14.2 EnsureSingleInstance 弹 InstanceConflictWindow，继续→关其它+等待退出",
          "public static bool EnsureSingleInstance()" in g14_si
          and "InstanceConflictWindow" in g14_si
          and "CloseOtherInstances();" in g14_si
          and "WaitOtherExit();" in g14_si)
    check("G14.3 App.OnStartup 调单实例，失败则 Shutdown（不再重复开）",
          "SingleInstance.EnsureSingleInstance()" in g14_app
          and "Shutdown();" in g14_app)
    check("G14.4 AppShutdown.StopAndRelease：先停循环流程，再逐个断开控制器",
          "FlowLoopManager.StopAll();" in g14_ash
          and "HardwareSetup.CardFamilies?.Disconnect(c)" in g14_ash)
    check("G14.5 MainWindow.Closing：拦下关闭→后台 Task 走进度条释放→Shutdown",
          "e.Cancel = true;" in g14_mw
          and "ClosingProgressWindow" in g14_mw
          and "AppShutdown.StopAndRelease();" in g14_mw
          and "dlg.Dispatcher.Invoke" in g14_mw
          and "Application.Current.Shutdown();" in g14_mw)
    check("G14.6 冲突窗：两个按钮「关闭其它并打开」/「取消打开」",
          "关闭其它并打开" in g14_icw and "取消打开" in g14_icw)
    check("G14.7 退出进度窗：含 ProgressBar",
          "ProgressBar" in g14_cpw)

# ---------------------------------------------------------------------
section("G15  图像生成点位：工具栏按钮 + 弹窗画布（导入底图 / 框内点选 / 折线 → 机器坐标点位）")
# ---------------------------------------------------------------------
g15_vm = read(r"ViewModels\GraphPointGenViewModel.cs")
g15_pvm = read(r"ViewModels\PointViewModel.cs")
g15_xaml = read(r"Views\PointPage.xaml")
g15_cs = read(r"Views\PointPage.xaml.cs")
g15_dlg = read(r"Views\GraphGenDialog.xaml")
g15_dlgcs = read(r"Views\GraphGenDialog.xaml.cs")
if (g15_vm is None or g15_pvm is None or g15_xaml is None or g15_cs is None
        or g15_dlg is None or g15_dlgcs is None):
    check("G15.0 找到 图形生成 / 点位页 / 弹窗 相关文件", False)
else:
    check("G15.0 找到 图形生成 / 点位页 / 弹窗 相关文件", True)
    check("G15.1 画布固定尺寸常量 CanvasW/CanvasH",
          "public const double CanvasW = 340;" in g15_vm and "public const double CanvasH = 260;" in g15_vm)
    check("G15.2 AddPoint 把点夹在画布范围内",
          "public void AddPoint(double x, double y)" in g15_vm
          and "Math.Max(0, Math.Min(CanvasW, x))" in g15_vm
          and "Math.Max(0, Math.Min(CanvasH, y))" in g15_vm)
    check("G15.3 ToMachine：起点偏移 + 按框尺寸线性映射（画布 Y 向下→机器 Y 向上）",
          "OriginX + c.X / CanvasW * FrameW" in g15_vm
          and "OriginY + (CanvasH - c.Y) / CanvasH * FrameH" in g15_vm)
    check("G15.4 折线采样 Resample（步长 >0 时密集采样）",
          "private static List<Point> Resample(List<Point> poly, double step)" in g15_vm
          and "Step > 0 && machine.Count >= 2" in g15_vm)
    check("G15.5 生成结果用 Generated 事件回传（机器坐标序列）",
          "public event Action<List<Point>>? Generated;" in g15_vm
          and "Generated?.Invoke(pts);" in g15_vm)
    check("G15.6 导入底图（OpenFileDialog + BitmapImage 立即读入）",
          "private void ImportImage()" in g15_vm
          and "new OpenFileDialog" in g15_vm
          and "BitmapCacheOption.OnLoad" in g15_vm)
    check("G15.7 PointViewModel 暴露 Graph 并订阅 Generated → 追加到当前工位",
          "public GraphPointGenViewModel Graph { get; } = new();" in g15_pvm
          and "Graph.Generated += AppendGraphPoints;" in g15_pvm
          and "private void AppendGraphPoints(List<Point> pts)" in g15_pvm)
    check("G15.8 生成写入 轴1←X、轴2←Y（Positions[0]/[1]）",
          "p.Positions[0].Position = Math.Round(pts[i].X, 4);" in g15_pvm
          and "p.Positions[1].Position = Math.Round(pts[i].Y, 4);" in g15_pvm)
    check("G15.9 点位页工具栏有「图像生成点位」按钮 → 命令 → 打开弹窗",
          "图像生成点位" in g15_xaml
          and "ImageGenPointsCommand" in g15_xaml
          and "ImageGenPointsCommand = new RelayCommand(_ => ImageGenPoints()" in g15_pvm
          and "var dlg = new GraphGenDialog { DataContext = Graph };" in g15_pvm)
    check("G15.10 弹窗内画布：绑定 Path/Points，含 导入底图 / 生成点位 按钮",
          'Data="{Binding Path}"' in g15_dlg
          and 'ItemsSource="{Binding Points}"' in g15_dlg
          and "导入底图" in g15_dlg and "生成点位" in g15_dlg)
    check("G15.11 画布单击 → 弹窗把画布坐标交给 VM 添加点位",
          'MouseLeftButtonDown="GraphCanvas_MouseLeftButtonDown"' in g15_dlg
          and "private void GraphCanvas_MouseLeftButtonDown" in g15_dlgcs
          and "vm.AddPoint(pos.X, pos.Y);" in g15_dlgcs)
    check("G15.12 悬浮卡已移除：点位页不再内嵌「图形生成点位」卡与画布单击处理",
          "图形生成点位" not in g15_xaml
          and "GraphCanvas_MouseLeftButtonDown" not in g15_xaml
          and "GraphCanvas_MouseLeftButtonDown" not in g15_cs)
    check("G15.13 弹窗不把 Owner 设成自己（首个窗口时 MainWindow 会返回自身 → 会抛异常）",
          "!ReferenceEquals(owner, this)" in g15_dlgcs)

# ---------------------------------------------------------------------
section("G16  工程师页 输入/输出/气缸 三列表：虚拟化 + 搜索过滤 + 取消按钮 + 单行紧凑行")
# ---------------------------------------------------------------------
g16 = read(r"Views\EngineerPage.xaml")
g16_vm = read(r"ViewModels\EngineerViewModel.cs")
if g16 is None or g16_vm is None:
    check("G16.0 找到 EngineerPage.xaml / EngineerViewModel.cs", False)
else:
    check("G16.0 找到 EngineerPage.xaml / EngineerViewModel.cs", True)
    check("G16.1 有 VirtualCardListStyle（ListBox 样式，IO 与气缸三列表共用）",
          '<Style x:Key="VirtualCardListStyle" TargetType="ListBox">' in g16)
    check("G16.2 开启虚拟化 + 容器回收 + 像素滚动",
          '<Setter Property="VirtualizingPanel.IsVirtualizing" Value="True"/>' in g16
          and '<Setter Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"/>' in g16
          and '<Setter Property="VirtualizingPanel.ScrollUnit" Value="Pixel"/>' in g16)
    check("G16.3 双向滚动条 Auto + CanContentScroll（虚拟化前提）",
          '<Setter Property="ScrollViewer.VerticalScrollBarVisibility" Value="Auto"/>' in g16
          and '<Setter Property="ScrollViewer.HorizontalScrollBarVisibility" Value="Auto"/>' in g16
          and '<Setter Property="ScrollViewer.CanContentScroll" Value="True"/>' in g16)
    check("G16.4 输入 / 输出 / 气缸 各一个虚拟化 ListBox，绑的是**过滤视图**",
          '<ListBox ItemsSource="{Binding InputsView}" Style="{StaticResource VirtualCardListStyle}">' in g16
          and '<ListBox ItemsSource="{Binding OutputsView}" Style="{StaticResource VirtualCardListStyle}">' in g16
          and '<ListBox ItemsSource="{Binding CylindersView}" Style="{StaticResource VirtualCardListStyle}">' in g16)
    check("G16.5 IO / 气缸 都不再用非虚拟化的 ItemsControl（WrapPanel）",
          '<ItemsControl ItemsSource="{Binding Inputs}">' not in g16
          and '<ItemsControl ItemsSource="{Binding Outputs}">' not in g16
          and '<ItemsControl ItemsSource="{Binding Cylinders}">' not in g16)
    check("G16.6 三个列表各有搜索框（绑 InputSearch / OutputSearch / CylinderSearch，实时过滤）",
          '{Binding InputSearch, UpdateSourceTrigger=PropertyChanged}' in g16
          and '{Binding OutputSearch, UpdateSourceTrigger=PropertyChanged}' in g16
          and '{Binding CylinderSearch, UpdateSourceTrigger=PropertyChanged}' in g16
          and "InputMatchInfo" in g16 and "OutputMatchInfo" in g16 and "CylinderMatchInfo" in g16)
    check("G16.7 过滤用**独立** CollectionViewSource（不能用默认视图，否则 IO 页被一起过滤）",
          "_inputsCvs.Source = Inputs;" in g16_vm
          and "_outputsCvs.Source = Outputs;" in g16_vm
          and "_cylindersCvs.Source = Cylinders;" in g16_vm
          and "CollectionViewSource" in g16_vm
          and "GetDefaultView" not in g16_vm)
    check("G16.8 搜索关键字为空时全量放行 + 匹配名称/功能/卡类/套码/控制器/卡号/模块",
          "if (string.IsNullOrWhiteSpace(keyword)) return true;" in g16_vm
          and "StringComparison.OrdinalIgnoreCase" in g16_vm)
    check("G16.9 卡片不再固定宽度（IO 210 / 气缸 224 都已移除，改为随列宽拉伸）",
          'Width="210"' not in g16 and 'Width="224"' not in g16)
    check("G16.10 行高压缩为单行（三处卡片内边距 10,5；IO 输出按钮与气缸按钮均 26 高）",
          g16.count('Padding="10,5"') >= 3
          and g16.count('Height="26" MinWidth="56"') >= 2)
    check("G16.11 三个搜索框右侧都有「取消」按钮（浅灰 pill，次要操作），各自绑 Clear*SearchCommand",
          '<Style x:Key="SearchClearBtn" TargetType="Button" BasedOn="{StaticResource TtPillGrayBtn}">' in g16
          and g16.count('Style="{StaticResource SearchClearBtn}"') == 3
          and 'Command="{Binding ClearInputSearchCommand}"' in g16
          and 'Command="{Binding ClearOutputSearchCommand}"' in g16
          and 'Command="{Binding ClearCylinderSearchCommand}"' in g16)
    check("G16.12 VM 里三个取消命令都已注册且清空对应关键字",
          "ClearInputSearchCommand = new RelayCommand(_ => InputSearch = string.Empty);" in g16_vm
          and "ClearOutputSearchCommand = new RelayCommand(_ => OutputSearch = string.Empty);" in g16_vm
          and "ClearCylinderSearchCommand = new RelayCommand(_ => CylinderSearch = string.Empty);" in g16_vm)
    check("G16.13 气缸搜索谓词 MatchCylinder：空关键字放行 + 覆盖名称/编号/类型/输出点/感应点",
          "private static bool MatchCylinder(CylinderRuntime? rt, string? keyword)" in g16_vm
          and "it.DeviceId" in g16_vm and "it.OutPoint" in g16_vm
          and "it.SensorExtend" in g16_vm and "it.SensorRetract" in g16_vm
          and g16_vm.count("if (string.IsNullOrWhiteSpace(keyword)) return true;") >= 2)
    check("G16.14 气缸行改为单行紧凑（名称 | 类型·输出点 | 按钮），不再用旧的三行卡片",
          'Text="{Binding Item.Name}"' in g16
          and '<Binding Path="Item.Type"/>' in g16
          and 'StringFormat=输出点：{0}' not in g16)
    # 布局：轴控制移到点位表正上方（同列相邻）；IO 换到加宽后的左列以保住两表宽度
    check("G16.15 轴控制与点位表同列相邻（轴控在点位表正上方），IO/气缸在另一列",
          '<!-- 右列：轴控制（上） + 点位移动和设置（下） -->' in g16
          and '<!-- 左列：IO 控制（上） + 气缸控制（下） -->' in g16
          and g16.index('<!-- 右列：轴控制') < g16.index('<!-- ① 轴控制')
          < g16.index('<!-- ④ 点位移动和设置')
          and g16.index('<!-- 左列：IO 控制') < g16.index('<!-- ② IO 控制')
          < g16.index('<!-- ③ 气缸控制')
          # 两列都必须是 star 宽度（自适应）。**不锁具体比例**：左宽右窄还是右宽左窄是布局口味，
          # 会随需求改（当前是 * / 2*，右列更宽给点位表），锁死会让每次调比例都误报。
          and g16.count('<ColumnDefinition Width="') >= 2
          and '<Grid Grid.Column="1" Margin="8,0,0,0">' in g16)

# ---------------------------------------------------------------------
section("G17  资源可达性：XAML 引用的 StaticResource 键必须真能解析到（防 AppleCheckBox 类崩溃）")
# ---------------------------------------------------------------------
# ★ 2026-10-07 真机崩溃：PointPage 里写了 Style="{StaticResource AppleCheckBox}"，该键只在
#   Themes\AppleControls.xaml 定义，而 App.xaml 只合并了 Resources\AppStyles.xaml →
#   InitializeComponent() 抛 XamlParseException「无法找到名为 AppleCheckBox 的资源」。
#   教训：**grep 到 x:Key 不等于运行时找得到**——必须沿 App.xaml 的 MergedDictionaries 递归求可达集。
import re as _re

_USE = _re.compile(r'\{(?:Static|Dynamic)Resource\s+([A-Za-z_][\w.]*)\s*\}')
_KEY = _re.compile(r'x:Key="([^"]+)"')
_SRC = _re.compile(r'Source="([^"]+)"')

# Themes\ / Resources\ 是「字典」本身，不实例化，内部引用在合并时解析 → 不扫
_RES_SKIP_DIRS = ("Themes", "Resources")
# 已知跨文件耦合（子视图借用宿主页面资源），非缺陷
_RES_ALLOW = {
    (r"Views\RunStatsView.xaml", "KpiTitle"),   # 由宿主 OperatorPage.xaml 提供
    (r"Views\RunStatsView.xaml", "KpiValue"),
}


def _res_keys(path, seen=None):
    """递归收集 App.xaml 可达字典链里的全部 x:Key（跳过 pack:// 外部程序集）。"""
    seen = seen or set()
    n = os.path.normpath(path)
    if n in seen or not os.path.exists(n):
        return set()
    seen.add(n)
    txt = open(n, "rb").read().decode("utf-8-sig")
    keys = set(_KEY.findall(txt))
    base = os.path.dirname(n)
    for src in _SRC.findall(txt):
        if src.startswith("pack:") or src.startswith("/"):
            continue
        keys |= _res_keys(os.path.join(base, src.replace("/", os.sep)), seen)
    return keys


_app_keys = _res_keys("App.xaml")
check("G17.0 App.xaml 可达资源链非空（读到 %d 个键）" % len(_app_keys), len(_app_keys) > 100)

_res_bad = []
_res_scanned = 0
for _dp, _dirs, _fs in os.walk(ROOT):
    _dirs[:] = [d for d in _dirs if d not in ("obj", "bin") and d not in _RES_SKIP_DIRS]
    for _f in _fs:
        if not _f.endswith(".xaml"):
            continue
        _p = os.path.join(_dp, _f)
        _rel = os.path.relpath(_p, ROOT)
        try:
            _txt = open(_p, "rb").read().decode("utf-8-sig")
        except Exception:
            continue
        _res_scanned += 1
        _own = set(_KEY.findall(_txt))
        for _k in sorted(set(_USE.findall(_txt))):
            if _k in _own or _k in _app_keys:
                continue
            if (_rel, _k) in _RES_ALLOW:
                continue
            _res_bad.append("%s :: %s" % (_rel, _k))

check("G17.1 扫了 %d 个 XAML，引用的 StaticResource 键全部可解析" % _res_scanned,
      not _res_bad, " | ".join(_res_bad[:8]))

# ---------------------------------------------------------------------
section("G18  相机层（海康 MVS）：真实取像接线 + 缺原生库时优雅降级")
# ---------------------------------------------------------------------
CAM_SVC = r"Services\Camera\MvsCameraService.cs"
CAM_VM = r"ViewModels\CameraViewModel.cs"
CAM_PAGE = r"Views\CameraPage.xaml"
CAM_DLL = r"Native\MvCameraControl.Net.dll"
VE = r"Services\Vision\VisionEngine.cs"

_svc = read(CAM_SVC)
_vm = read(CAM_VM)
_page = read(CAM_PAGE)
_csproj = read("NoCodeMotion.csproj")
_ve = read(VE)
_has_dll = os.path.exists(os.path.join(ROOT, CAM_DLL))

check("G18.0 找到相机服务 / 相机 VM / 相机页 / 托管封装",
      all(x is not None for x in (_svc, _vm, _page)) and _has_dll,
      "svc=%s vm=%s page=%s dll=%s" % (_svc is not None, _vm is not None,
                                       _page is not None, _has_dll))

check("G18.1 csproj 以 HintPath 引 Native\\MvCameraControl.Net.dll（CopyLocal，随程序发布）",
      _csproj is not None
      and 'Include="MvCameraControl.Net"' in _csproj
      and r"Native\MvCameraControl.Net.dll" in _csproj,
      "缺 Reference 或 HintPath")

if _svc is not None:
    check("G18.2 会话复用：按设备 Key 缓存已打开会话（一台 GigE 设备只有一个独占句柄）",
          "_sessions" in _svc and "TryGetValue(device.Key" in _svc)
    check("G18.3 缺原生库不抛：捕获 DllNotFoundException 并写可读 LastError",
          "DllNotFoundException" in _svc and "MVS 运行库" in _svc)
    check("G18.4 位数不匹配（BadImageFormat）也有专门提示",
          "BadImageFormatException" in _svc)
    check("G18.5 曝光换算：界面毫秒 → SDK 微秒（×1000）",
          "ExposureMs*1000" in _svc or "ExposureMs * 1000" in _svc)
    check("G18.6 触发三态映射（连续 / 软触发 / 硬触发 → TriggerMode On|Off + TriggerSource）",
          '"软触发"' in _svc and '"硬触发"' in _svc
          and 'TriggerSource", "Software"' in _svc and 'TriggerSource", "Line0"' in _svc)

if _vm is not None:
    check("G18.7 CameraViewModel 暴露 Preview / HasPreview / SearchDevicesCommand",
          "public ImageSource? Preview" in _vm and "HasPreview" in _vm
          and "SearchDevicesCommand" in _vm)
    check("G18.8 Connect / Capture 走真实取像并回退仿真（失败不谎报已连接）",
          "TryGrabBgra" in _vm and "VisionSimCapture" in _vm
          and "SelectedItem.IsConnected = false;" in _vm)

if _page is not None:
    check("G18.9 相机页绑定 Preview 预览 + SearchDevicesCommand 按钮",
          "{Binding Preview}" in _page and "{Binding SearchDevicesCommand}" in _page)

if _ve is not None:
    check("G18.10 流程视觉先走 MVS 再回退（与相机页共用同一会话）",
          "MvsCameraService.TryGrabBgra" in _ve and "ResolveMvsDevice" in _ve)
    check("G18.11 最后一层回退也被 try 包住：CaptureFrame 永不抛（OpenCV 缺原生库也不中断流程）",
          "ManagedPlaceholder" in _ve)

# ---------------------------------------------------------------------
section("G19  名称库分组（下拉框二级菜单）：超过阈值按 '-' 前缀分类")
# ---------------------------------------------------------------------
# 需求：轴 / IO / 气缸 / 变量的名称超过 20 个时，下拉框按名称里 '-' 前缀分类
#      （下料-x / 下料-y / 上料-x → 分类「下料」「上料」）。
# ★ 分组挂在名称库的**默认视图**上 —— 与 G16.7「过滤必须用私有视图」正好相反：
#   过滤是各页条件不同（共享会互相污染），分组是所有消费者都想要的同一件事（共享才对）。
GRP = r"Services\NamePrefixGroupDescription.cs"
BEH = r"Views\NameGroupHeaderBehavior.cs"
CATS = r"Services\Catalog.cs"
STYLES = r"Resources\AppStyles.xaml"
PP = r"Views\PointPage.xaml"

_grp = read(GRP)
_beh = read(BEH)
_cats = read(CATS)
_sty = read(STYLES)
_pp = read(PP)

check("G19.0 找到 分组依据 / 附加属性 / 目录 / 样式 / 点位页",
      all(x is not None for x in (_grp, _beh, _cats, _sty, _pp)))

if _grp is not None:
    check("G19.1 分组依据继承 GroupDescription，按第一个连字符取前缀（半角 + 全角）",
          "class NamePrefixGroupDescription : GroupDescription" in _grp
          and "name.IndexOfAny(Separators)" in _grp
          and r"char[] Separators = { '-', '\uff0d' };" in _grp)
    check("G19.2 截不出前缀时归入「其他」；条目仍是 string（不包成新对象，SelectedItem 才不会失配）",
          'FallbackGroupName = "其他"' in _grp
          and "PrefixOf(item as string)" in _grp)

if _cats is not None:
    check("G19.3 阈值 = 20，且给**默认视图**挂分组（不是私有视图）",
          "public const int GroupThreshold = 20;" in _cats
          and "CollectionViewSource.GetDefaultView(target)" in _cats
          and "view.GroupDescriptions.Add(new NamePrefixGroupDescription())" in _cats)
    check("G19.4 分组覆盖 轴 / IO（输入·输出·合并）/ 气缸 / 变量 + 并集 AllNames",
          all(n in _cats for n in ("yield return AxisNames;", "yield return IoNames;",
                                   "yield return InIoNames;", "yield return OutIoNames;",
                                   "yield return CylinderNames;", "yield return VariableNames;",
                                   "yield return AllNames;")))
    check("G19.5 名称库每次重建后都刷新分组（RebuildAll 末尾挂钩）",
          "RefreshGrouping();" in _cats
          and "private static void RebuildAll()" in _cats
          and _cats.index("private static void RebuildAll()") < _cats.index("RefreshGrouping();"))
    check("G19.6 只在「超过阈值 + 至少一个名字能截出前缀」时开分组（否则退化成一个「其他」大组）",
          "target.Count > GroupThreshold" in _cats
          and "NamePrefixGroupDescription.PrefixOf(n) != null" in _cats)

if _beh is not None:
    check("G19.7 用附加属性而非 Style Setter（GroupStyle 是只读集合、不是依赖属性，Setter 写不了）",
          "DependencyProperty.RegisterAttached(" in _beh
          and "combo.GroupStyle.Add(" in _beh
          and 'HeaderTemplateKey = "NameGroupHeaderTemplate"' in _beh)
    check("G19.10 全局类处理器：所有页面的 ComboBox 一加载就自动挂分组头（不靠逐页 / 逐个 Style 去开）",
          "EventManager.RegisterClassHandler(" in _beh
          and "typeof(ComboBox)" in _beh
          and "FrameworkElement.LoadedEvent" in _beh
          # 必须 ModuleInitializer：静态构造函数要等第一次访问本类才跑，漏一行引用就会静默失效
          and "ModuleInitializer" in _beh
          and "static NameGroupHeader()" not in _beh)
    check("G19.11 类处理器与附加属性共用同一个 EnsureHeader（重复挂由 GroupStyle.Count 挡住）",
          _beh.count("EnsureHeader(combo)") == 2
          and "if (combo.GroupStyle.Count > 0) return;" in _beh)

if _sty is not None:
    check("G19.8 分组头模板存在，且 CellComboStyle 打开了它（没有分组时无任何副作用）",
          'x:Key="NameGroupHeaderTemplate"' in _sty
          and 'Property="b:NameGroupHeader.Enable" Value="True"' in _sty
          and "{Binding ItemCount" in _sty)
    check("G19.12 气缸页用的 AppleCombo 基于 CellComboStyle（继承 Enable Setter，该页自动生效）",
          'x:Key="AppleCombo"' in _sty
          and 'BasedOn="{StaticResource CellComboStyle}"' in _sty)

if _pp is not None:
    check("G19.9 点位表「期望值」下拉用的是内联样式，已单独开启分组头",
          'b:NameGroupHeader.Enable="True"' in _pp
          and 'xmlns:b="clr-namespace:NoCodeMotion.Behaviors"' in _pp)

if _sty is not None:
    # ★ 分组下拉的条目宿主必须是 ItemsPresenter。裸 <StackPanel IsItemsHost="True"/> 只实体化
    #   最外层的 GroupItem（= 分组标题），组内条目一个都不生成 → 下拉里只剩「轴 (2)」「其他 (30)」
    #   两行标题，真实名称一个都点不到（用户截图即此现象）。build / 类型检查都抓不到这个运行时坑。
    _cs = _sty.index('x:Key="CellComboStyle"')
    _pi = _sty.index('x:Name="PART_Popup"', _cs)
    _popup = _sty[_pi:_sty.index("</Popup>", _pi)]
    import re as _re
    _popup = _re.sub(r"<!--.*?-->", "", _popup, flags=_re.S)   # 注释里会引用反面写法，先剥掉
    check("G20 分组下拉的条目宿主是 ItemsPresenter（裸 IsItemsHost 面板不会生成组内条目）",
          "<ItemsPresenter" in _popup and "IsItemsHost" not in _popup)

if _sty is not None:
    # ★ 级联二级菜单：一级只列分类行，悬停/点击 → 右侧飞出二级菜单。两个部件名 row / flyout
    #   由 Behaviors.NameGroupFlyout 在运行时按名字查找 —— 名字一改行为就**静默失效**
    #   （不报错、不抛异常，只是悬停/点击毫无反应），所以必须锁住。
    _casc = _sty[_sty.index('<Style x:Key="NameGroupCascadeContainerStyle"'):]
    _casc = _casc[:_casc.index("</Style>")]
    check("G21.1 一级分类行样式存在：作用于 GroupItem，且开着 NameGroupFlyout 行为",
          'TargetType="GroupItem"' in _casc
          and 'Property="b:NameGroupFlyout.Enable" Value="True"' in _casc)
    check("G21.2 一级行是 ToggleButton（悬停/点击的载体）且带右侧箭头",
          'x:Name="row"' in _casc and "<ToggleButton" in _casc and "<Path" in _casc)
    check("G21.3 二级菜单 Popup 从**右侧**飞出，里面才是组内条目（ItemsPresenter）",
          'x:Name="flyout"' in _casc and 'Placement="Right"' in _casc
          and "<ItemsPresenter" in _casc)
    check("G21.4 二级菜单自行管理开关（StaysOpen），悬停进出不会闪",
          'StaysOpen="True"' in _casc)
    check("G21.5 一级行的内容/模板来自 GroupItem（TemplateBinding），不是写死的",
          'Content="{TemplateBinding Content}"' in _casc
          and 'ContentTemplate="{TemplateBinding ContentTemplate}"' in _casc)

if _beh is not None:
    check("G21.6 行为与 XAML 的部件名 / 资源键必须一致（改一处就静默失效）",
          'RowPartName = "row"' in _beh and 'FlyoutPartName = "flyout"' in _beh
          and 'ContainerStyleKey = "NameGroupCascadeContainerStyle"' in _beh)
    check("G21.7 EnsureHeader 同时挂上一级分类行样式（只挂 HeaderTemplate 就退化成平铺）",
          "gs.ContainerStyle = container" in _beh)
    check("G21.8 悬停与点击两种展开都要有，且点击收起后不被悬停立刻弹开",
          "row.MouseEnter" in _beh and "row.Click" in _beh and "suppressHover" in _beh)
    check("G21.9 外层下拉一关就把二级菜单收掉（Popup 不会自己跟着消失）",
          "gi.Unloaded" in _beh)

# ---------------------------------------------------------------------
# G22 示例工程「工位-对象」命名扩展 + 命名约定提示
# ---------------------------------------------------------------------
# 需求：新建工程弹窗里的示例模板要「轴 / IO / 气缸 / 变量尽量多」，且都用「-」命名，
#       好让名称下拉框的级联二级菜单一建工程就能看到；同时要把这套命名约定提示给用户。
# ★ 这里守的是几处**静默失效**：常量一改就退化成不分组；厂商一写成「模拟卡」，
#   WenQiZhiCardBridge.CanServeProject() 会把所有模板的默认硬件通道从雷赛封装改成卡族层
#   （不报错，只是行为整体变了）；轴号不唯一会让同一张卡上两根轴共用物理通道。
_cat = read(r"Services\ProjectTemplateCatalog.cs")
_tpl = read(r"Models\ProjectTemplate.cs")

if _cat is not None:
    _c = _cat.replace("\r\n", "\n")
    _i = _c.find("public static void ExpandNameLists(ProjectData d)")
    _exp = _c[_i:_c.index("// 点位构造助手", _i)] if _i >= 0 else ""
    check("G22.1 示例工程有 ExpandNameLists（工位-对象 名称扩展）",
          _i >= 0 and _exp != "")
    check("G22.2 五类对象都用「工位-对象」命名（轴/入/出/缸/变量）",
          '$"{st}-轴{i}"' in _exp and '$"{st}-入{i}"' in _exp and '$"{st}-出{i}"' in _exp
          and '$"{st}-缸{i}"' in _exp and '$"{st}-{sfx}"' in _exp)
    check("G22.3 每类都超过分组阈值 20（3 工位 × 8 = 24）",
          'DemoPerStation = 8' in _c
          and '"上料", "搬运", "下料"' in _c)
    check("G22.4 扩展轴卡的厂商必须是「雷赛」（写成模拟卡会把默认硬件通道改成卡族层）",
          '"雷赛", "DMC-E3000"' in _exp and "模拟卡" not in _exp)
    check("G22.5 扩展卡内轴号唯一（axisNo 递增，重复轴号会共用物理通道）",
          "axisNo++" in _exp)
    check("G22.6 空白工程不追加示例名称（它的定位就是所有页面均为空）",
          "ExpandSampleNames = false" in _c)

if _tpl is not None:
    _p = _tpl.replace("\r\n", "\n")
    check("G22.7 ProjectTemplate 有 ExpandSampleNames 开关且 Build() 用它守住扩展",
          "public bool ExpandSampleNames { get; init; } = true;" in _p
          and "if (ExpandSampleNames) Services.ProjectTemplateCatalog.ExpandNameLists(data);" in _p)

# ---- 命名约定提示：新建工程弹窗 + 轴/IO/气缸/变量 四页 ----
_np = read(r"Views\NewProjectDialog.xaml")
if _np is not None:
    check("G22.8 新建工程弹窗提示了「工位-对象」命名约定与分组规则",
          "工位-对象" in _np and "会自动变成下拉框里的分组标题" in _np)

# ★ 文案通俗化后：轴 / IO / 气缸 / 变量 四页各自的 PageHintBar 已合并成主窗口底部的
#   「本页怎么用」说明栏，文案集中在 Services\PageGuideCatalog.cs（一处改全站生效）。
#   这里守的是「合并之后没丢东西」：
#     G22.9a 主窗口确实挂了说明栏（在页面区和状态栏之间）；
#     G22.9b 切页会刷新它；
#     G22.9c 15 个页签一个都不能漏（漏了那页就是空白条）；
#     G22.9d 「工位-对象」命名约定还在；
#     G22.9e 说明里不许出现内部实现名（用户看不懂，也没法照着操作）。
_mw = (read(r"MainWindow.xaml") or "").replace("\r\n", "\n")
_mc = (read(r"MainWindow.xaml.cs") or "").replace("\r\n", "\n")
check("G22.9a 主窗口底部挂了「本页怎么用」说明栏（页面区与状态栏之间）",
      '<views:PageHintBar Grid.Row="2" x:Name="PageGuide"' in _mw
      and '<views:StatusBarView Grid.Row="3"/>' in _mw)
check("G22.9b 切页时刷新说明栏（Navigate 收尾调 ShowPageGuide，文案取 PageGuideCatalog）",
      "ShowPageGuide(key);" in _mc and "PageGuideCatalog.For(key)" in _mc)

_pgc = read(r"Services\PageGuideCatalog.cs")
if _pgc is None:
    check("G22.9 页签白话说明表存在", False, r"Services\PageGuideCatalog.cs 缺失")
else:
    _g = _pgc.replace("\r\n", "\n")

    def _guide_block(key):
        """切出 ["key"] = new Guide { ... } 这一段（到下一个条目或表尾）。"""
        mark = '["%s"] = new Guide' % key
        i = _g.find(mark)
        if i < 0:
            return ""
        nxt = _g.find('"] = new Guide', i + len(mark))
        end = _g.find("};", i) if nxt < 0 else nxt - 1
        return _g[i:end]

    _nav_keys = ["ProjectManager", "AxisController", "Axis", "Io", "Cylinder", "Point",
                 "Comm", "Tray", "Camera", "Variable", "Flow", "Engineer", "Custom",
                 "Operator", "Manual"]
    _miss = [k for k in _nav_keys if not _guide_block(k)]
    check("G22.9c 主窗口 15 个页签全都有白话说明（漏一页 = 那页底部空白条）",
          not _miss, "缺: " + ",".join(_miss))
    for _key, _tag in [("Axis", "轴"), ("Io", "IO"), ("Cylinder", "气缸"), ("Variable", "变量")]:
        check("G22.9d [%s] 页说明仍写了「工位-对象」命名约定" % _tag,
              "工位-对象" in _guide_block(_key))
    check("G22.9e 说明文案里不许出现内部实现名（类名 / 协议标准号，用户看不懂）",
          all(w not in _g for w in ("WenQiZhiCardBridge", "CardFamilyCatalog", "HardwareBridge",
                                    "GetCardAxisAlarmState", "SEMI E37", "ProjectStore")))

# ---- 仿真变量步骤：变量名里的「-」必须按名精确取值，不能拼进表达式 ----
_sf = read(r"Services\SimFlowPlayer.cs")
if _sf is not None:
    _s = _sf.replace("\r\n", "\n")
    check("G22.10 仿真变量步骤不再把变量名拼进表达式（({name}) 模式已删除，否则 - 命名会被分词成减号误算）",
          "({name})" not in _s)
    check("G22.11 仿真变量步骤按变量名精确取值（GetVariableResolved(a.VarName)，名称含 - 也安全）",
          "GetVariableResolved(a.VarName)" in _s and "VarOp = isSet ? null : vop" in _s)
    check("G22.12 取反保持「1 - cur」语义（不是算术 -cur；布尔 0↔1 精确）",
          "1 - cur" in _s)

# ---- 模板轴加速/减速时间必须是 0.2 秒（旧「加速度值」语义的 50/100/... 会让新建工程轴爬行）----
if _cat is not None:
    import re as _re
    _ax = _re.findall(r'Ax\([^()]*,\s*\d+(?:\.\d+)?,\s*(\d+),\s*(\d+)\)\);', _c)
    _bad = [(a, b) for a, b in _ax if '.' not in a and '.' not in b]
    check("G22.13 所有模板轴 Ax(...) 的加速/减速时间都是 0.2 秒（无遗留旧「加速度值」整数）",
          len(_bad) == 0, f"遗留旧值: {_bad[:5]}")
    check("G22.14 至少有轴使用了 0.2 秒斜坡默认值",
          _c.count("0.2, 0.2));") >= 1)

# ---------------------------------------------------------------------
# G23 流程视觉「字符识别」(OCR) —— Windows.Media.Ocr
# ---------------------------------------------------------------------
# 需求：视觉流程要有 OCR 字符识别工具。这里守的是几处**静默失效**：
#   ① 类型没进 VisualStepTypes → 下拉选不到；② 参数卡没绑 IsOcr → 右侧不显示；
#   ③ VisionEngine 的 switch 没这个 case → 跑到就报「未知步骤类型」；
#   ④ 目标框架不带 Windows SDK 版本 → Windows.Media.Ocr 编译不过。
_vfs = read(r"Models\VisualFlowStep.cs")
if _vfs is not None:
    _v = _vfs.replace("\r\n", "\n")
    check("G23.1 视觉步骤有字符识别（OCR）字段与识别区域回显",
          "public string OcrLanguage" in _v and "public string OcrExpectedText" in _v
          and "public string OcrMatchMode" in _v and "public bool OcrIgnoreCase" in _v
          and "public int OcrRoiX" in _v and "public int OcrRoiW" in _v
          and "public string OcrRoiText" in _v)

_vfp = read(r"Views\VisualFlowPage.xaml")
if _vfp is not None:
    _x = _vfp.replace("\r\n", "\n")
    _vfd = (read(r"Views\VisualFlowDetailViewModel.cs") or "").replace("\r\n", "\n")
    check("G23.2 视觉流程的工具类型含「字符识别」（候选在详情 VM 的 BasicStepTypes，步骤类型下拉才选得到）",
          "BasicStepTypes" in _vfd and '"字符识别"' in _vfd)
    check("G23.3 视觉流程页有 OCR 参数卡（语言/匹配方式/期望文本/忽略大小写/识别区域 + 清除）",
          'x:Key="OcrLanguages"' in _x and 'x:Key="OcrMatchModes"' in _x
          and "Binding IsOcr," in _x
          and "SelectedStep.OcrLanguage" in _x and "SelectedStep.OcrExpectedText" in _x
          and "SelectedStep.OcrMatchMode" in _x and "SelectedStep.OcrIgnoreCase" in _x
          and "ClearOcrRoiCommand" in _x)

_vm = read(r"Views\VisualFlowDetailViewModel.cs")
if _vm is not None:
    _m = _vm.replace("\r\n", "\n")
    check("G23.4 详情 VM 有 IsOcr 标志与「清除识别区域」命令",
          'public bool IsOcr => SelectedStep?.StepType == "字符识别";' in _m
          and "nameof(IsOcr)" in _m and "ClearOcrRoiCommand" in _m)

_ve = read(r"Services\Vision\VisionEngine.cs")
if _ve is not None:
    _e = _ve.replace("\r\n", "\n")
    check("G23.5 引擎 switch 处理「字符识别」并调用 RunOcr",
          'case "字符识别":' in _e and "RunOcr(s, cur, report, progress, display)" in _e
          and "private static void RunOcr(" in _e)
    check("G23.6 引擎用 Windows.Media.Ocr 实现（语言解析 + 期望文本比对）",
          "Windows.Media.Ocr.OcrEngine" in _e and "ResolveOcrEngine" in _e
          and "EvaluateOcr" in _e and "SoftwareBitmap.CreateCopyFromBuffer" in _e)
    check("G23.7 字符识别结果带结构化 Text（供后续节点/显示）",
          'public string Text { get; set; } = "";' in _e and "Text = recognized," in _e)

_csproj = read("NoCodeMotion.csproj")
if _csproj is not None:
    check("G23.8 目标框架含 Windows SDK 版本（否则 Windows.Media.Ocr 编译不过）",
          "net10.0-windows10.0.19041.0" in _csproj)

_ai = read(r"Services\AiProjectExchange.cs")
if _ai is not None:
    _a = _ai.replace("\r\n", "\n")
    check("G23.9 AI 交换：字符识别类型归一 + 导出",
          'return "字符识别";' in _a and 'case "字符识别":' in _a)

# G24 图像采集「来源」：默认相机 + 真实相机取像 + 路径无效明确报错
_vfs = read(r"Models\VisualFlowStep.cs")
if _vfs is not None:
    _v = _vfs.replace("\r\n", "\n")
    check("G24.1 图像采集默认来源=相机（不再默认「文件」）",
          'private string _sourceType = "相机";' in _v)

_ve = read(r"Services\Vision\VisionEngine.cs")
if _ve is not None:
    _e = _ve.replace("\r\n", "\n")
    check("G24.2 引擎按相机名/编号解析相机下标（ResolveCameraIndex）",
          "public static int ResolveCameraIndex(string? cameraId)" in _e
          and "ProjectStore.Data?.Cameras" in _e
          and "n.Where(char.IsDigit)" in _e)
    check("G24.3 采集走真实相机：MVS 优先 + OpenCV 兜底，CaptureFrame 复用同一 helper",
          "private static byte[]? TryGrabRealCamera(" in _e
          and "MvsCameraService.TryGrabBgra" in _e
          and _e.count("TryGrabRealCamera(") >= 3)
    check("G24.4 来源=文件/文件夹 路径无效 → 明确失败（不再静默回退测试图）",
          "来源=文件 但未设置文件路径（不再回退测试图）" in _e
          and "图像文件不存在：" in _e
          and "文件夹内没有可读图像：" in _e
          and "未提供有效图像路径" not in _e)
    check("G24.5 采集失败后 cur 可为 null（Clone 容错 + 下游判空）",
          "display = cur?.Clone();" in _e
          and _e.count('AddFail(report, s, "请先执行图像采集")') >= 5)

_tpl = read(r"Services\ProjectTemplateCatalog.cs")
if _tpl is not None:
    _t = _tpl.replace("\r\n", "\n")
    check("G24.6 模板视觉采集步 来源=相机 + 相机名（不再把文件夹写进 SavePath）",
          'Name = "图像采集", StepType = "图像采集", SourceType = "相机", CameraId = "上视相机"' in _t
          and 'SourceType = "相机",\n                    CameraId = "下视相机",' in _t
          and 'SavePath = "Images/inspection/"' not in _t)

_x = read(r"Views\VisualFlowPage.xaml")
if _x is not None:
    check("G24.7 视觉流程页「相机」为可编辑下拉（绑 CameraNames，也可手填编号）",
          'ItemsSource="{Binding CameraNames}"' in _x
          and 'Text="{Binding SelectedStep.CameraId, UpdateSourceTrigger=PropertyChanged}"' in _x
          and 'IsEditable="True" IsReadOnly="False"' in _x)

_vm2 = read(r"Views\VisualFlowDetailViewModel.cs")
if _vm2 is not None:
    _m2 = _vm2.replace("\r\n", "\n")
    check("G24.8 详情 VM 暴露工程相机名列表（CameraNames）",
          "public IReadOnlyList<string> CameraNames" in _m2
          and "nameof(CameraNames)" in _m2)

_ng = read(r"Services\Vision\NgVisionExecutor.cs")
if _ng is not None:
    _g = _ng.replace("\r\n", "\n")
    check("G24.9 节点图采集节点：当帧为空时明确报错（不拿 0×0 帧继续跑算子）",
          "if (r is { Ok: false } && !rep.HasImage)" in _g)

_ai2 = read(r"Services\AiProjectExchange.cs")
if _ai2 is not None:
    _a2 = _ai2.replace("\r\n", "\n")
    check("G24.10 AI prompt 说明：来源=文件 必须给文件路径",
          "必须同时给" in _a2
          and "图像采集的「来源」取 相机/文件/文件夹" in _a2)

# G25 视觉流程页结果图：滚轮缩放 / 中键拖拽平移 / 双击复位
_zp = read(r"Views\ZoomPanBehavior.cs")
if _zp is None:
    check("G25.1 存在可复用的 ZoomPanBehavior（滚轮缩放 + 中键拖拽平移 + 双击复位）", False, "文件缺失")
else:
    _z = _zp.replace("\r\n", "\n")
    check("G25.1 存在可复用的 ZoomPanBehavior（滚轮缩放 + 中键拖拽平移 + 双击复位）",
          "public static class ZoomPanBehavior" in _z
          and "namespace NoCodeMotion.Behaviors" in _z
          and "MouseButton.Middle" in _z
          and "MouseWheel" in _z
          and "ClickCount == 2" in _z
          and "public static void Reset(FrameworkElement el)" in _z)
    check("G25.2 变换顺序 Scale → Translate，且原点取左上角",
          "tg.Children.Add(new ScaleTransform(1, 1));" in _z
          and "tg.Children.Add(new TranslateTransform(0, 0));" in _z
          and "el.RenderTransformOrigin = new Point(0, 0);" in _z)
    check("G25.3 鼠标位移必须在父空间量（禁止直接用 e.GetPosition(el) 做平移基准）",
          "private static Point GetStablePos(FrameworkElement el, MouseEventArgs e)" in _z
          and "el.Parent as IInputElement ?? el" in _z
          and "e.GetPosition(relative)" in _z)
    check("G25.4 光标锚定缩放的解析式 + 中键/左键各司其职",
          "mouseX * (1 - s) + offsetX * s" in _z
          and "public static (double X, double Y) PanOffset(" in _z
          and "e.ChangedButton != MouseButton.Middle" in _z
          and "handledEventsToo: true" in _z)

_vfx = read(r"Views\VisualFlowPage.xaml")
if _vfx is not None:
    _x2 = _vfx.replace("\r\n", "\n")
    check("G25.5 视觉流程页结果图启用行为 + 外层 Border 裁剪 + 操作说明",
          'xmlns:beh="clr-namespace:NoCodeMotion.Behaviors"' in _x2
          and 'beh:ZoomPanBehavior.IsEnabled="True"' in _x2
          and 'ClipToBounds="True"' in _x2
          and "中键拖拽平移" in _x2 and "双击复位" in _x2 and "滚轮缩放" in _x2)

_vfc = read(r"Views\VisualFlowPage.xaml.cs")
if _vfc is not None:
    _c2 = _vfc.replace("\r\n", "\n")
    check("G25.6 框选只认左键单击（中键/双击让给缩放平移行为）",
          "if (e.ChangedButton != MouseButton.Left || e.ClickCount != 1) return;" in _c2)

# G25.7 运行结果标注颜色：缺陷框 + 测量线/标记为绿色（不再红/蓝）；OCR 的 NG 仍红
_ve = read(r"Services\Vision\VisionEngine.cs")
if _ve is not None:
    _e = _ve.replace("\r\n", "\n")
    _def_green = "Cv.Cv2.Rectangle(dst, r, Rgb(30, 170, 80), 2);" in _e
    _measure_no_blue = "Rgb(40, 120, 240)" not in _e           # 测量三条原本是蓝，已全部改绿
    _ocr_ng_red = "pass ? Rgb(30, 170, 80) : Rgb(220, 40, 40)" in _e  # 通过绿/不通过红保留
    check("G25.7 运行结果标注统一为绿色（缺陷框+测量线/标记=绿；OCR 的 NG 仍红）",
          _def_green and _measure_no_blue and _ocr_ng_red)

# G26 字符识别结果叠加：每行文字画绿框 + 框上方绿字标签（与模板匹配同一套矢量叠加思路）
_mb = read(r"Models\MatchBox.cs")
if _mb is not None:
    _m = _mb.replace("\r\n", "\n")
    check("G26.1 存在 TextBoxItem / TextOverlayBox（字符识别框模型 + 屏幕坐标投影）",
          "public sealed class TextBoxItem" in _m
          and "public sealed class TextOverlayBox" in _m
          and "public static TextOverlayBox Project(TextBoxItem tb, double scale, double offsetX, double offsetY)" in _m
          and "public string Text { get; set; }" in _m)

if _ve is not None:
    check("G26.2 VisionReport 单独收集逐行文字框 TextBoxes（不并入 Matches 污染匹配统计）",
          "public List<TextBoxItem> TextBoxes { get; } = new();" in _e
          and _e.count("report.Matches.Add(") == 1)
    check("G26.3 RunOcr 逐行取词框（OcrWord.BoundingRect）并按 ROI 原点偏移回整图坐标",
          "foreach (var line in ocr.Lines)" in _e
          and "foreach (var word in line.Words)" in _e
          and "word.BoundingRect" in _e
          and "report.TextBoxes.Add(new TextBoxItem" in _e
          and "Left = bx + rx," in _e and "Top = by + ry," in _e)

_vm2 = read(r"Views\VisualFlowDetailViewModel.cs")
if _vm2 is not None:
    _v2 = _vm2.replace("\r\n", "\n")
    check("G26.4 VM 暴露 OcrTextBoxes / TextOverlayBoxes 两个 DP，三处运行入口都回填",
          "public ObservableCollection<TextBoxItem>? OcrTextBoxes" in _v2
          and "public ObservableCollection<TextOverlayBox>? TextOverlayBoxes" in _v2
          and _v2.count("OcrTextBoxes = report.TextBoxes.Count > 0") == 3)

if _vfc is not None:
    check("G26.5 页面把文字框投影成屏幕坐标（与匹配框共用同一 scale/offset）",
          "var texts = _vm.OcrTextBoxes;" in _c2
          and "TextOverlayBox.Project(x, scale, offsetX, offsetY)" in _c2
          and "HookOcrTextCollection" in _c2)

if _vfx is not None:
    check("G26.6 结果图新增 TextOverlay 叠加层：绿框 + 框上方文字标签",
          'x:Name="TextOverlay"' in _x2
          and 'ItemsSource="{Binding TextOverlayBoxes}"' in _x2
          and 'Visibility="{Binding HasTextOverlayBoxes' in _x2
          and 'Text="{Binding Text}"' in _x2)

# ---------------------------------------------------------------------
section("G27  视觉标定：9 点 XY 仿射 + 5 点旋转圆拟合（模型 / 求解器 / 检测 / 执行器 / 接线 / 落盘 / UI）")
# ---------------------------------------------------------------------


def col_names_of(src, start_marker, stop_prefix):
    """从某个 Build*Sheet 方法体里按顺序抽出 dt.Columns.Add("名字") 的列名。"""
    names = []
    started = False
    for ln in src.replace("\r\n", "\n").split("\n"):
        if not started:
            if start_marker in ln:
                started = True
            continue
        if ln.strip().startswith(stop_prefix):
            break
        key = 'dt.Columns.Add("'
        i = ln.find(key)
        if i >= 0:
            j = ln.find('"', i + len(key))
            if j > i:
                names.append(ln[i + len(key):j])
    return names


def has_dashed_calib_var(src):
    """标定变量名里绝不能出现减号（表达式求值器把 '-' 当减运算符）。"""
    i = 0
    while True:
        i = src.find('"标定_', i)
        if i < 0:
            return False
        j = src.find('"', i + 1)
        if j < 0:
            return False
        if "-" in src[i + 1:j]:
            return True
        i = j + 1


_cc = read(r"Models\CameraCalibration.cs")
if _cc is None:
    check("G27.1 找到 Models\\CameraCalibration.cs", False, "文件缺失")
else:
    check("G27.1 标定结果模型：仿射换算 + 绕旋转中心换算 + 可用性判定",
          "public sealed class CameraCalibration" in _cc
          and "public bool IsUsable => IsValid && PixelEquivalent > 0;" in _cc
          and "public void ImageToMachine(double u, double v, out double x, out double y)" in _cc
          and "x = A1 * u + A2 * v + A3;" in _cc
          and "y = B1 * u + B2 * v + B3;" in _cc
          and "public void ImageToMachineRotated(double u, double v, double angleDeg, out double x, out double y)" in _cc
          and "double du = u - RotCenterU, dv = v - RotCenterV;" in _cc
          and "ImageToMachine(ru, rv, out x, out y);" in _cc)
    check("G27.2 字段全标量（xlsx 反射导出「标定」表要能直接落盘；点集用字符串存）",
          "public string CameraName { get; set; }" in _cc
          and "public double PixelEquivalent { get; set; }" in _cc
          and "public double RotCenterU { get; set; }" in _cc
          and "public string Points9 { get; set; }" in _cc
          and "public string Machine9 { get; set; }" in _cc
          and "public string Points5 { get; set; }" in _cc)

_cs = read(r"Services\Vision\Calibration\CalibSolver.cs")
if _cs is None:
    check("G27.3 找到 CalibSolver.cs", False, "文件缺失")
else:
    check("G27.3 仿射最小二乘：正规方程 + 高斯消元；像素当量/方向角/残差都有解析式",
          "public static AffineFit FitAffine(IReadOnlyList<(double U, double V, double X, double Y)> pts)" in _cs
          and "m[0, 0] = suu; m[0, 1] = suv; m[0, 2] = su;" in _cs
          and "if (!Solve3(m, new[] { sux, svx, sx }, cx))" in _cs
          and "r.PixelEquivalentU = Math.Sqrt(r.A1 * r.A1 + r.B1 * r.B1);" in _cs
          and "r.AngleXDeg = Math.Atan2(r.B1, r.A1) * 180.0 / Math.PI;" in _cs
          and "r.RmsMm = Math.Sqrt(se / (2.0 * n));" in _cs
          and "private static bool Solve3(" in _cs)
    check("G27.4 圆拟合（Kåsa 线性化）+ 不变文化格式化（小数点不被本地化）",
          "public static CircleFit FitCircle(IReadOnlyList<(double U, double V)> pts)" in _cs
          and "double z = p.U * p.U + p.V * p.V;" in _cs
          and "if (!Solve3(m, new[] { -suz, -svz, -sz }, sol))" in _cs
          and "double cu = -d / 2.0, cv = -e / 2.0;" in _cs
          and "r.RmsPx = Math.Sqrt(se / n);" in _cs
          and "CultureInfo.InvariantCulture" in _cs)
    check("G27.5 求解器不依赖 OpenCV / WPF（标定是最需要离线断言的一环）",
          "OpenCvSharp" not in _cs and "Cv." not in _cs and "System.Windows" not in _cs)

_md = read(r"Services\Vision\Calibration\MarkerDetector.cs")
if _md is None:
    check("G27.6 找到 MarkerDetector.cs", False, "文件缺失")
else:
    check("G27.6 标记质心检测：亮度阈值 + 4 连通域 + 质心/填充率/圆度 + 面积降序",
          "public static List<MarkerBlob> Detect(byte[]? bgra, int width, int height, MarkerDetectOptions? opt = null)" in _md
          and "public static MarkerBlob? DetectLargest(" in _md
          and "int lum = (r * 299 + g * 587 + b * 114) / 1000;" in _md
          and "int b = bgra[i * 4], g = bgra[i * 4 + 1], r = bgra[i * 4 + 2];" in _md
          and "CenterU = (double)su / area," in _md
          and "list.Sort((a, b) => b.Area.CompareTo(a.Area));" in _md)
    check("G27.7 检测器刻意纯托管（opencv_world480.dll 不在搜索路径时冒烟也要能跑）",
          "OpenCvSharp" not in _md and "Cv." not in _md)

_cr = read(r"Services\Vision\Calibration\CalibrationRunner.cs")
if _cr is None:
    check("G27.8 找到 CalibrationRunner.cs", False, "文件缺失")
else:
    check("G27.8 执行器：硬件/采集全走注入委托（冒烟可用合成数据端到端跑完整流程）",
          "public sealed class CalibrationRunner" in _cr
          and "public Func<string, double>? ReadAxisPosition { get; set; }" in _cr
          and "public Action<string, double>? MoveAxisAbs { get; set; }" in _cr
          and "public Func<(byte[]? Bgra, int Width, int Height)>? GrabFrame { get; set; }" in _cr
          and "public Action? Guard { get; set; }" in _cr)
    check("G27.9 9 点铺 3×3 网格；5 点以「基准角为中心」按步距绕旋转轴",
          "for (int iy = -1; iy <= 1; iy++)" in _cr
          and "for (int ix = -1; ix <= 1; ix++)" in _cr
          and "double tx = baseX + ix * o.PitchMm;" in _cr
          and "double start = baseAngle - (count - 1) / 2.0 * o.RotationStepDeg;" in _cr)
    check("G27.10 停止/暂停必须冒泡（OperationCanceledException 一律重抛，绝不吞成「标定失败」）",
          _cr.count("catch (OperationCanceledException)") >= 3
          and _cr.count("catch (OperationCanceledException) { throw; }") >= 2
          and "不能当成「标定失败」吞掉" in _cr)
    check("G27.11 可中断等待（每 ≤50ms 轮询 Guard）+ 残差/半径闸门",
          "int step = Math.Min(50, ms - elapsed);" in _cr
          and "if (fit.RmsMm > o.MaxResidualMm)" in _cr
          and "if (circ.RadiusPx < o.MinRotationRadiusPx)" in _cr
          and "if (circ.RmsPx > o.MaxRotationResidualPx)" in _cr)
    check("G27.12 取不到图就明确失败（不回退合成图；合成图会悄悄产出垃圾标定参数）",
          'return (null, "采集图像失败（相机不可用或返回空帧）");' in _cr)

_cst = read(r"Services\Vision\Calibration\CalibrationStore.cs")
if _cst is None:
    check("G27.13 找到 CalibrationStore.cs", False, "文件缺失")
else:
    check("G27.13 标定仓库按相机名增改查（数据落 ProjectData.Calibrations，随工程落盘）",
          "public static ObservableCollection<CameraCalibration>? All => ProjectStore.Data?.Calibrations;" in _cst
          and "public static CameraCalibration? FindUsable(string? cameraName)" in _cst
          and "public static CameraCalibration? Upsert(CameraCalibration? rec)" in _cst
          and "public static bool HasUsable(string? cameraName)" in _cst)

_cve = read(r"Services\Vision\Calibration\CalibVarExport.cs")
if _cve is None:
    check("G27.14 找到 CalibVarExport.cs", False, "文件缺失")
else:
    check("G27.14 导出 7 个标定变量，变量名一律不带减号（'-' 会被表达式求值器当减号）",
          "public const string VarPixelEquivalent = \"标定_像素当量\";" in _cve
          and "public const string VarRotCenterV = \"标定_旋转中心V\";" in _cve
          and _cve.count("public const string Var") == 7
          and not has_dashed_calib_var(_cve))
    check("G27.15 变量单元格：同名复用 → 空槽复用 → 新开一行（WriteVarRow 只改不建）",
          "private static bool EnsureCell(string name, out VariableRow row, out int col)" in _cve
          and "var nr = new VariableRow();" in _cve
          and "SimRuntime.SetVariable(name, v);" in _cve)

_ve27 = read(r"Services\Vision\VisionEngine.cs")
if _ve27 is None:
    check("G27.16 找到 VisionEngine.cs", False, "文件缺失")
else:
    _e27 = _ve27.replace("\r\n", "\n")
    check("G27.16 引擎接线：case「标定」→ RunCalibration；模板匹配按标定输出机台真实位置",
          "using NoCodeMotion.Services.Vision.Calibration;" in _e27
          and 'case "标定":' in _e27
          and "RunCalibration(s, report, progress);" in _e27
          and "CameraCalibration? calib = CalibrationStore.FindUsable(calibCam);" in _e27
          and _e27.count("calib.ImageToMachineRotated(") == 2
          and "public static string CameraNameOf(int cameraIndex)" in _e27)
    check("G27.17 标定会驱动真实轴：仿真桩拒绝 + 轴名找不到就失败（绝不拿轴 0 顶上）+ 不回退合成图",
          "if (bridge is StubHardwareBridge)" in _e27
          and "标定会驱动真实轴，已中止" in _e27
          and "标定的 X/Y 轴名在工程里找不到" in _e27
          and "标定的旋转轴名在工程里找不到" in _e27
          and "return real == null ? ((byte[]?)null, 0, 0) : (real, gw, gh);" in _e27)
    check("G27.18 引擎运行路径不吞停止信号（标定期间按停止必须真的停下来）",
          _e27.count("catch (OperationCanceledException)") >= 2)

_ax = read(r"Services\AiProjectExchange.cs")
if _ax is None:
    check("G27.19 找到 AiProjectExchange.cs", False, "文件缺失")
else:
    _a = _ax.replace("\r\n", "\n")
    _ns = _a.split("private static string NormalizeStepType")[1] if "private static string NormalizeStepType" in _a else ""
    check("G27.19 AI 交换：标定步骤可导出 / 可回填（标定方式 / X轴 / Y轴 / 旋转轴 / 间距mm …）",
          'case "标定":' in _a
          and 'sb.Append(", \\"标定方式\\": ").Append(J(s.CalibMode));' in _a
          and 'st.CalibMode = Str(s, "标定方式", "calibMode") ?? st.CalibMode;' in _a
          and 'st.CalibRotAxis = Str(s, "旋转轴", "标定旋转轴", "calibRotAxis") ?? st.CalibRotAxis;' in _a)
    check("G27.20 类型归一化：标定必须排在「相机 → 图像采集」之前（AI 常写「相机标定」）",
          't.Contains("标定")' in _ns
          and 't.Contains("采集")' in _ns
          and _ns.find('t.Contains("标定")') < _ns.find('t.Contains("采集")'))

_vfs = read(r"Models\VisualFlowStep.cs")
if _vfs is None:
    check("G27.21 找到 VisualFlowStep.cs", False, "文件缺失")
else:
    check("G27.21 视觉步骤模型新增 14 个标定字段（方式/三轴/间距/步距/点数/速度/延时/检测参数/结果文本）",
          "public string CalibMode" in _vfs
          and "public string CalibXAxis" in _vfs
          and "public string CalibYAxis" in _vfs
          and "public string CalibRotAxis" in _vfs
          and "public double CalibPitchMm" in _vfs
          and "public double CalibRotationStepDeg" in _vfs
          and "public int CalibRotationCount" in _vfs
          and "public double CalibSpeed" in _vfs
          and "public int CalibSettleMs" in _vfs
          and "public int CalibThreshold" in _vfs
          and "public bool CalibDarkMarker" in _vfs
          and "public int CalibMinArea" in _vfs
          and "public int CalibMaxArea" in _vfs
          and "public string CalibResultText" in _vfs)

_pd = read(r"Models\ProjectData.cs")
if _pd is None:
    check("G27.22 找到 ProjectData.cs", False, "文件缺失")
else:
    check("G27.22 工程数据新增 Calibrations 集合（一台相机一条）",
          "public ObservableCollection<CameraCalibration> Calibrations { get; set; } = new();" in _pd)

_xs = read(r"Services\XlsxProjectStore.cs")
if _xs is None:
    check("G27.23 找到 XlsxProjectStore.cs", False, "文件缺失")
else:
    _flow_cols = col_names_of(_xs, "private static DataTable BuildFlowSheet", "private static ")
    check("G27.23 落盘：新增「标定」工作表 + 菜单顺序（料盘/相机/标定/变量/流程…）",
          '["Calibrations"] = "标定",' in _xs
          and '"料盘", "相机", "标定", "变量", "流程", "工程师", "自定义", "操作员",' in _xs)
    check("G27.24 流程表新增 14 个标定列（导出与回填一一对应）",
          all(c in _flow_cols for c in
              ("标定方式", "标定X轴", "标定Y轴", "标定旋转轴", "标定间距mm", "旋转步距",
               "旋转点数", "标定速度", "标定稳定ms", "标记阈值", "暗标记",
               "标记最小面积", "标记最大面积", "标定结果"))
          and 'SetStr(r, "标定方式", vObj, "CalibMode");' in _xs
          and 'CalibRotAxis = r["标定旋转轴"]?.ToString() ?? "",' in _xs)
    check("G27.25 顺手补上 OCR 字段的落盘（此前只在 AI 交换 JSON 里有，xlsx 会静默丢配置）",
          all(c in _flow_cols for c in
              ("识别语言", "期望文本", "匹配方式", "忽略大小写", "识别框X", "识别框Y", "识别框W", "识别框H"))
          and 'SetStr(r, "识别语言", vObj, "OcrLanguage");' in _xs
          and 'OcrLanguage = r["识别语言"]?.ToString() ?? "自动",' in _xs)
    _key = 'Add("流程（'
    _i = _xs.find(_key)
    _j = _xs.find("列）", _i)
    _legend_n = int(_xs[_i + len(_key):_j]) if (_i >= 0 and _j > _i) else -1
    check("G27.26 合并页列说明的「流程（N列）」必须与 BuildFlowSheet 的真实列数一致（防图例漂移）",
          _legend_n == len(_flow_cols),
          "图例写 %d 列，实际 %d 列" % (_legend_n, len(_flow_cols)))

_vm27 = read(r"Views\VisualFlowDetailViewModel.cs")
if _vm27 is None:
    check("G27.27 找到 VisualFlowDetailViewModel.cs", False, "文件缺失")
else:
    _v27 = _vm27.replace("\r\n", "\n")
    check("G27.27 VM：标定类型开关 + 轴名候选 + 当前标定回显，并在切步/改结果时刷新",
          'public bool IsCalibration => SelectedStep?.StepType == "标定";' in _v27
          and "public IReadOnlyList<string> CalibAxisNames" in _v27
          and "public string CalibCurrentText" in _v27
          and "OnPropertyChanged(nameof(IsCalibration));" in _v27
          and "OnPropertyChanged(nameof(CalibCurrentText));" in _v27)

_vfx27 = read(r"Views\VisualFlowPage.xaml")
if _vfx27 is None:
    check("G27.28 找到 VisualFlowPage.xaml", False, "文件缺失")
else:
    _x27 = _vfx27.replace("\r\n", "\n")
    check("G27.28 视觉流程页：标定参数卡（方式/相机/三轴/9 点间距/旋转步距/标记阈值/开始标定）",
          'x:Key="CalibModes"' in _x27
          and 'Text="标定参数"' in _x27
          and 'Text="标定方式"' in _x27
          and 'Text="旋转轴"' in _x27
          and 'Text="开始标定"' in _x27
          and 'ItemsSource="{Binding CalibAxisNames}"' in _x27
          and 'Text="{Binding CalibCurrentText}"' in _x27
          and 'Text="{Binding SelectedStep.CalibResultText}"' in _x27
          and 'Visibility="{Binding IsCalibration, Converter={StaticResource BoolToVis}}"' in _x27)

_mb27 = read(r"Models\MatchBox.cs")
if _mb27 is not None:
    _m27 = _mb27.replace("\r\n", "\n")
    check("G27.29 匹配框带机台真实位置（未标定时 RealText 明确说「未标定」，不假装有值）",
          "public double RealX { get; set; } = double.NaN;" in _m27
          and "public bool HasReal { get; set; }" in _m27
          and 'public string RealText => HasReal ? $"({RealX:F2}, {RealY:F2}) mm" : "未标定";' in _m27)

# ---------------------------------------------------------------------
section("G28  真实 SECS/HSMS 通讯（SEMI E37 会话 + E5 报文；防「假收发」回归）")
# ---------------------------------------------------------------------
_SECS28 = r"Services\Hardware\Comm\Secs"
_si28 = read(_SECS28 + r"\SecsItem.cs")
_sm28 = read(_SECS28 + r"\SecsMessage.cs")
_hs28 = read(_SECS28 + r"\HsmsSession.cs")
_sc28 = read(_SECS28 + r"\SecsCommChannel.cs")
_cm28 = read(r"Services\Hardware\Comm\CommManager.cs")
_ci28 = read(r"Models\CommItem.cs")
_cv28 = read(r"ViewModels\CommViewModel.cs")
_cx28 = read(r"Views\CommPage.xaml")
_ax28 = read(r"Services\AiProjectExchange.cs")

_pairs28 = (("SecsItem.cs", _si28), ("SecsMessage.cs", _sm28),
            ("HsmsSession.cs", _hs28), ("SecsCommChannel.cs", _sc28))
_miss28 = [n for n, v in _pairs28 if v is None]
check("G28.1 四个 SECS 新文件都在（SecsItem / SecsMessage / HsmsSession / SecsCommChannel）",
      not _miss28, "缺失：" + ", ".join(_miss28))
_ph28 = [n for n, v in _pairs28 if v is not None and ("@@HDR@@" in v or "@@FTR@@" in v)]
check("G28.2 新文件都已封页眉页脚水印（没有 @@HDR@@ / @@FTR@@ 占位符残留）",
      not _ph28, "仍有占位符：" + ", ".join(_ph28))

# ---- 数据项编解码 ----
_FMT28 = {"List": 0, "Binary": 8, "Boolean": 9, "Ascii": 16, "Jis8": 17,
          "I8": 24, "I1": 25, "I2": 26, "I4": 28,
          "F8": 32, "F4": 36, "U8": 40, "U1": 41, "U2": 42, "U4": 44}
if _si28 is None:
    check("G28.3 SecsFormat 枚举值 == SEMI E5 的 6 位格式码", False, "文件缺失")
else:
    _b28 = [k for k, v in _FMT28.items() if ("%s = %d," % (k, v)) not in _si28]
    check("G28.3 SecsFormat 枚举值 == SEMI E5 的 6 位格式码（15 个，含 L=0 / A=16 / U4=44）",
          not _b28, "缺失或值不对：" + ", ".join(_b28))
    check("G28.4 数据项首字节 = (格式码 << 2) | 长度字节数",
          "outp.Add((byte)(((int)Format << 2) | lenBytes));" in _si28)
    check("G28.5 长度字段语义：L 写子项数、A/JIS8 写字节数、其余写元素数",
          "case SecsFormat.List: return Children.Count;" in _si28
          and "case SecsFormat.Jis8: return Raw == null ? 0 : Raw.Length;" in _si28)
    check("G28.6 ★ 静态工厂叫 Numbers / Floats（叫 Ints / Reals 会和同名实例属性撞 CS0102）",
          "public static SecsItem Numbers(SecsFormat format, params long[] values)" in _si28
          and "public static SecsItem Floats(SecsFormat format, params double[] values)" in _si28
          and "public static SecsItem Ints(" not in _si28
          and "public static SecsItem Reals(" not in _si28)
    check("G28.7 SML 往返：ToSml / ParseSml 都在，空列表写 <>",
          "public string ToSml()" in _si28
          and "public static SecsItem ParseSml(string text, ref int pos, out string error)" in _si28
          and 'return "<L>";' in _si28)

# ---- 报文头编解码 ----
_ST28 = {"DataMessage": 0, "SelectReq": 1, "SelectRsp": 2, "DeselectReq": 3, "DeselectRsp": 4,
         "LinktestReq": 5, "LinktestRsp": 6, "RejectReq": 7, "SeparateReq": 9}
if _sm28 is None:
    check("G28.8 HsmsSType 控制码 == E37 取值", False, "文件缺失")
else:
    _b28 = [k for k, v in _ST28.items() if ("%s = %d," % (k, v)) not in _sm28]
    check("G28.8 HsmsSType 控制码 == E37 取值（Separate.req=9；★ 没有 Separate.rsp / 没有 8）",
          not _b28 and "SeparateRsp" not in _sm28, "缺失：" + ", ".join(_b28))
    check("G28.9 头 10 字节：byte2=W-bit(0x80)/状态，byte3=Stream，byte4=PType 0，byte5=Function/SType",
          "public const int HeaderLength = 10;" in _sm28
          and "frame[2] = IsData ? (byte)(WBit ? 0x80 : 0x00) : Status;" in _sm28
          and "frame[3] = IsData ? Stream : (byte)0;" in _sm28
          and "frame[4] = 0;" in _sm28
          and "frame[5] = IsData ? Function : (byte)SType;" in _sm28)
    check("G28.10 ★ 控制消息判定必须同时要求 Stream(byte3)==0（只看 byte5 会把 S1F1 当成 Select.req）",
          "bool isControl = hb3 == 0 && Array.IndexOf(ControlTypes, maybe) >= 0;" in _sm28)
    check("G28.11 控制消息会话号固定 0xFFFF（数据消息才用 DeviceID）",
          "public const ushort ControlSessionId = 0xFFFF;" in _sm28)
    check("G28.12 SML 解析认控制关键字（SELECT / DESELECT / LINKTEST / SEPARATE）",
          'case "SELECT": return HsmsSType.SelectReq;' in _sm28
          and 'case "SEPARATE": return HsmsSType.SeparateReq;' in _sm28)

# ---- HSMS 会话层 ----
if _hs28 is None:
    check("G28.13 HSMS 会话层齐备", False, "文件缺失")
else:
    check("G28.13 会话层：主动/被动两角色 + T3/T5/T6/T7/T8 超时齐备",
          "public bool Active { get; set; }" in _hs28
          and "public int T3Ms { get; set; } = 45000;" in _hs28
          and "public int T5Ms { get; set; } = 10000;" in _hs28
          and "public int T6Ms { get; set; } = 5000;" in _hs28
          and "public int T7Ms { get; set; } = 10000;" in _hs28
          and "public int T8Ms { get; set; } = 5000;" in _hs28)
    check("G28.14 会话层：Select / Linktest 就地处理 + 自动应答 S1F1→S1F2、S1F13→S1F14、S1F15→S1F16、S1F17→S1F18",
          "case HsmsSType.SelectReq:" in _hs28
          and "case HsmsSType.LinktestReq:" in _hs28
          and "return SecsMessage.Data(DeviceId, 1, 2, false, msg.SystemBytes, body);" in _hs28
          and "return SecsMessage.Data(DeviceId, 1, 14, false, msg.SystemBytes," in _hs28
          and "if (msg.Function == 15) return SecsMessage.Data(DeviceId, 1, 16, false, msg.SystemBytes, SecsItem.B(0));" in _hs28
          and "if (msg.Function == 17) return SecsMessage.Data(DeviceId, 1, 18, false, msg.SystemBytes, SecsItem.B(0));" in _hs28)
    check("G28.15 ★ 无应答的控制消息不能等（Separate.req / Reject.req 没有应答，要用 ReplyType() 判断）",
          "bool waitReply = msg.IsData ? msg.WBit : msg.ReplyType().HasValue;" in _hs28
          and "bool waitReply = msg.IsControl || msg.WBit;" not in _hs28)
    check("G28.16 ★ 通讯会话线程不碰 HardwareBridge.WaitGuard（通讯与运动互不阻塞）",
          "HardwareBridge.WaitGuard" not in _hs28)
    check("G28.17 单帧长度上限 + 非法长度立即断开（防畸形长度字段吃内存）",
          "public const int MaxFrameLength = 16 * 1024 * 1024;" in _hs28
          and "if (len < SecsMessage.HeaderLength || len > MaxFrameLength)" in _hs28)
    check("G28.18 被动模式暴露实际监听端口（BaudOrPort 填 0 时由系统分配，测试/显示都靠它）",
          "public int BoundPort { get; private set; }" in _hs28
          and "BoundPort = ((IPEndPoint)listener.LocalEndpoint).Port;" in _hs28)

# ---- SECS 通道 ----
if _sc28 is None:
    check("G28.19 SECS 通道齐备", False, "文件缺失")
else:
    check("G28.19 SECS 通道实现 ICommChannel：Send 收 SML、Recv 出单行 SML",
          "public sealed class SecsCommChannel : ICommChannel" in _sc28
          and "SecsMessage.ParseSml(data.Trim()" in _sc28
          and "return m == null ? string.Empty : m.ToSmlLine();" in _sc28)
    check("G28.20 ★ W-bit 应答会被会话层按 SystemBytes 吃掉，通道必须额外给 SendAndReply 把应答交回调用方",
          "public string SendAndReply(string data)" in _sc28
          and "return reply == null ? null : reply.ToSmlLine();" in _sc28)

# ---- CommManager 接线 ----
if _cm28 is None:
    check("G28.21 CommManager 接线", False, "文件缺失")
else:
    _c28 = _cm28.replace("\r\n", "\n")
    _i_secs = _c28.find('if (Contains(type, "SECS", "HSMS", "E37", "E5"))')
    _i_modbus = _c28.find('if (Contains(type, "ModbusRTU"')
    _i_def = _c28.find("// 默认按 TCP 处理")
    check("G28.21 ★ SECS 分支必须在 Modbus / 默认 TCP 之前（落到最后会被当普通 TCP，静默失去 SECS 语义）",
          0 <= _i_secs < _i_modbus < _i_def,
          "secs=%d modbus=%d default=%d" % (_i_secs, _i_modbus, _i_def))
    check("G28.22 CommManager 提供 SendAndReply / Peek / TryPeek / Close",
          "public string SendAndReply(CommItem cfg, string data)" in _c28
          and "public ICommChannel Peek(CommItem cfg)" in _c28
          and "public ICommChannel TryPeek(CommItem cfg)" in _c28
          and "public void Close(CommItem cfg)" in _c28)
    check("G28.23 ★ 页面刷状态必须用 TryPeek（Peek 会真 Create，而 S7/三菱MC 的 Create 直接抛异常）",
          "★ 页面刷新状态必须用它" in _c28)
    check("G28.24 SECS 会话日志接到同一出口（后台线程的收发，页面 / Lua 要看得到）",
          "if (ch is SecsCommChannel secs) secs.Log = Log;" in _c28)
    check("G28.25 通道指纹含 SECS 角色 / DeviceID（改了要重建通道，不能复用旧连接）",
          '+ $"|{cfg.SecsRole}|{cfg.SecsDeviceId}"' in _c28)
    # 管道 / 共享内存 / 本机文件 / Web 这几类的参数改了同样必须换通道实例（否则改完还是旧连接行为）
    check("G28.25b 通道指纹含本机互联与接口类参数（管道角色 / 共享区大小 / 写入方式 / 请求方式 / Content-Type）",
          '+ $"|{cfg.PipeRole}|{cfg.ShmSizeKb}|{cfg.FileWriteMode}|{cfg.WebMethod}|{cfg.WebContentType}' in _c28)
    # 仿真开关也必须进指纹：不然「开/关仿真」不会重建通道，界面显示在仿真、底下还是老连接（或反之）
    check("G28.25c 通道指纹含仿真开关（开了仿真不能沿用实机连接）",
          '{(cfg.SimEnabled ? "仿真" : "实机")}' in _c28)

# ---- 模型参数 ----
if _ci28 is None:
    check("G28.26 CommItem 的 SECS 标量参数", False, "文件缺失")
else:
    _need28 = ["SecsRole", "SecsDeviceId", "SecsT3Ms", "SecsT5Ms", "SecsT6Ms",
               "SecsT7Ms", "SecsT8Ms", "SecsAutoReply", "SecsMdln", "SecsSoftRev"]
    _b28 = [n for n in _need28 if n not in _ci28]
    check("G28.26 CommItem 有 10 个 SECS 标量属性（反射导出会自动带进 xlsx「通讯」表）",
          not _b28, "缺失：" + ", ".join(_b28))
    check("G28.27 类型注释补了 SECS(HSMS)（下拉候选外的值渲染空白）",
          "SECS(HSMS)" in _ci28)

# ---- 通讯页 VM ----
if _cv28 is None:
    check("G28.28 通讯页 VM 接真收发", False, "文件缺失")
else:
    check("G28.28 通讯类型下拉含 SECS(HSMS)",
          '"SECS(HSMS)"' in _cv28)
    check("G28.29 ★ 不再有「打开连接只改个 bool」「发送只回显」的假实现",
          "回显仿真" not in _cv28
          and "« 回应：" not in _cv28
          and "IsConnected = true;" not in _cv28)
    check("G28.30 打开 / 发送 / 接收 / 关闭 都走 CommManager（与 Lua 同一条真实通道）",
          "_comm.Peek(item)" in _cv28
          and "_comm.SendAndReply(item, txt)" in _cv28
          and "_comm.Recv(item)" in _cv28
          and "_comm.Close(item)" in _cv28)
    check("G28.31 ★ 日志必须 marshal 回 UI 线程（HSMS 读线程直接回调，ObservableCollection 跨线程会抛）",
          "private static void PostToUi(Action action)" in _cv28
          and "app.Dispatcher.BeginInvoke(action);" in _cv28
          and "PostToUi(() =>" in _cv28)
    check("G28.32 SECS 会话操作齐备：建立会话 / 探活 / SML 预设",
          "SecsSelectCommand" in _cv28 and "SecsLinktestCommand" in _cv28
          and "ApplySecsPresetCommand" in _cv28
          and "public ObservableCollection<string> SecsPresets" in _cv28)

# ---- 通讯页 XAML ----
if _cx28 is None:
    check("G28.33 通讯页 XAML", False, "文件缺失")
else:
    _x28 = _cx28.replace("\r\n", "\n")
    _i_p = _x28.find("ConverterParameter='网口TCP|网口UDP|ModbusTCP|相机网口")
    _j_p = _x28.find("'", _i_p + len("ConverterParameter='"))
    _param28 = _x28[_i_p + len("ConverterParameter='"):_j_p] if _i_p >= 0 else ""
    check("G28.33 ★ 端口/IP 芯片的 ConverterParameter 必须含 SECS(HSMS)（转换器是精确匹配，漏了永远不显示）",
          "SECS(HSMS)" in _param28, "实际参数=%r" % _param28)
    check("G28.34 SECS 参数卡 + 建立会话 / 探活 / 接收 按钮 + 会话状态回显",
          'Text="SECS / HSMS 参数（半导体设备用的那套通讯）"' in _x28
          and 'Text="建立会话"' in _x28
          and 'Text="探活"' in _x28
          and 'Text="接收"' in _x28
          and 'Text="{Binding SecsStateText}"' in _x28)
    _n28 = _x28.count("SECS(HSMS)")
    check("G28.35 SECS(HSMS) 在页面里出现 ≥5 处（端口/IP + 端口号 + 参数卡 + 两个按钮）",
          _n28 >= 5, "count=%d" % _n28)

# ---- AI 工程交换 ----
if _ax28 is None:
    check("G28.36 AI 工程交换带 SECS 字段", False, "文件缺失")
else:
    _a28 = _ax28.replace("\r\n", "\n")
    check("G28.36 AI 工程交换：通讯项带 SECS 字段（角色 / DeviceID / T3..T8 / 自动应答 / MDLN / SOFTREV）",
          "SecsRole = Str(e," in _a28 and "SecsDeviceId = IntDef(e," in _a28
          and "SecsT3Ms = IntDef(e," in _a28 and "SecsT8Ms = IntDef(e," in _a28
          and "SecsAutoReply = BoolDef(e," in _a28
          and "SecsMdln = Str(e," in _a28 and "SecsSoftRev = Str(e," in _a28)
    check("G28.37 AI 工程交换：新增 BoolDef 助手（认 JSON 布尔，也认 1/0 与 true/false/是/否）",
          "private static bool BoolDef(JsonElement e, bool def, params string[] aliases)" in _a28)

# ---------------------------------------------------------------------
section("G29  气缸单动 / 双动（类型归一化 + 单动收起「缩回输出」）+ 连接控制器后 IO 名称库同刷")
# ---------------------------------------------------------------------
_CYL = r"Models\CylinderItem.cs"
_CVM = r"ViewModels\CylinderViewModel.cs"
_ACVM = r"ViewModels\AxisControllerViewModel.cs"
_CX = r"Views\CylinderPage.xaml"
_LS = r"Services\Hardware\Leadshine\LeadshineHardwareBridge.cs"
_WQ = r"Services\Hardware\Cards\WenQiZhiCardBridge.cs"
_AIX = r"Services\AiProjectExchange.cs"
_TPL = r"Services\ProjectTemplateCatalog.cs"
_ANN = r"Services\ProjectJsonAnnotator.cs"
_OM = r"Views\OperatorManualPage.xaml.cs"
_SMK = r".smoke\Program.cs"

# ---- 气缸类型：归一化 + 单动语义 ----
_cy = read(_CYL)
if _cy is None:
    check("G29.0 找到 CylinderItem.cs", False, _CYL)
else:
    check("G29.0 找到 CylinderItem.cs", True)
    check("G29.1 有 public static string NormalizeType(string? raw)",
          "public static string NormalizeType(string? raw)" in _cy)
    check("G29.2 归一化认「单动 / 单作 / 单」→ 单动气缸",
          's.Contains("单动") || s.Contains("单作")' in _cy
          and 'if (s.Contains("单")) return "单动气缸";' in _cy)
    check("G29.3 归一化认「双动 / 双作 / 兜底」→ 双动气缸",
          's.Contains("双动") || s.Contains("双作")' in _cy
          and 'if (s.Length == 0) return "双动气缸";' in _cy)
    check("G29.4 默认类型 = 双动气缸", 'private string _type = "双动气缸";' in _cy)
    check("G29.5 Type 的 setter 走 NormalizeType，并补通知 IsSingleActing",
          "set { if (SetField(ref _type, NormalizeType(value))) OnPropertyChanged(nameof(IsSingleActing)); }" in _cy)
    check("G29.6 有 IsSingleActing（单动 = 只有 1 路输出）",
          'public bool IsSingleActing => NormalizeType(_type) == "单动气缸";' in _cy)
    check("G29.7 不再有旧默认值 _type = \"双作用\"", '_type = "双作用"' not in _cy)

_cvm = read(_CVM)
check("G29.8 类型候选 = 单动气缸 / 双动气缸（旧的单作用 / 双作用已移除）",
      _cvm is not None
      and 'public string[] TypeOptions { get; } = { "单动气缸", "双动气缸" };' in _cvm)

# ---- 连接控制器后：按方向拆分的两个 IO 名称库必须一起刷新（气缸页选不了点的根因） ----
_acvm = read(_ACVM)
if _acvm is None:
    check("G29.9 GenerateIoPoints 三库同刷", False, "文件缺失")
else:
    _g = _acvm.replace("\r\n", "\n")
    i0 = _g.find("private static string GenerateIoPoints")
    i1 = _g.find("private static IoItem MakeIo")
    seg = _g[i0:i1] if 0 <= i0 < i1 else ""
    check("G29.9 GenerateIoPoints 同时刷新 InIoNames / OutIoNames / IoNames",
          "Catalog.SetIoIn(data.Inputs" in seg
          and "Catalog.SetIoOut(data.Outputs" in seg
          and "Catalog.SetIo(data.Inputs" in seg)
    check("G29.10 该处有 ★ 注释说明「只刷合并库 → 气缸页选不了 IO 名称」",
          "★ 三个名称库必须一起刷新" in seg)

# ---- 运行时：单动气缸不写第 2 路输出 ----
for _lbl, _f in (("雷赛", _LS), ("卡族", _WQ)):
    _b = read(_f)
    _b2 = _b.replace("\r\n", "\n") if _b else ""
    check("G29.11 %s CylinderMove：只有双动气缸才写第 2 路输出（BackupSensor）" % _lbl,
          "if (!cyl.IsSingleActing)" in _b2
          and "var retIo = FindIo(cyl.BackupSensor, isOutput: true);" in _b2)

# ---- 气缸页：单动收起「缩回输出」两处 ----
_x = read(_CX)
if _x is None:
    check("G29.12 气缸页单动收起「缩回输出」", False, _CX)
else:
    _x2 = _x.replace("\r\n", "\n")
    check("G29.12 气缸页：IsSingleActing 触发器 2 处（IO 配置卡 + 状态显示卡）",
          _x2.count('Binding="{Binding SelectedItem.IsSingleActing}"') == 2,
          "count=%d" % _x2.count('Binding="{Binding SelectedItem.IsSingleActing}"'))
    check("G29.13 触发器命中时 Value=Collapsed（两处都收起）",
          _x2.count('<Setter Property="Visibility" Value="Collapsed"/>') == 2
          and _x2.count('Value="True">') == 2)
    _ind14 = [len(l) - len(l.lstrip(" ")) for l in _x2.split("\n") if 'Text="缩回输出"/>' in l]
    check("G29.14 两处「缩回输出」都已包进折叠容器（缩进加深到 36 格，不再是 32 格裸行）",
          _ind14 == [36, 36], "indents=%r" % (_ind14,))

# ---- 文案 / 默认类型 ----
_aix = read(_AIX)
check("G29.15 AI 工程交换：气缸默认类型 = 双动气缸（旧「双作用」已清除）",
      _aix is not None and '?? "双动气缸",' in _aix and '"双作用"' not in _aix)
_tpl = read(_TPL)
check("G29.16 工程模板 / 示例：不再出现「双作用」", _tpl is not None and "双作用" not in _tpl)
_ann = read(_ANN)
check("G29.17 工程 JSON 注释：气缸类型口径已换成单动 / 双动气缸",
      _ann is not None and "单动气缸" in _ann and "双作用" not in _ann)
_om = read(_OM)
check("G29.18 操作手册文案：单动气缸 / 双动气缸",
      _om is not None and "单动气缸" in _om and "双作用" not in _om)

# ---- 冒烟覆盖 ----
_sm = read(_SMK)
if _sm is None:
    check("G29.19 冒烟段 T 覆盖气缸单动 / 双动", False, _SMK)
else:
    check("G29.19 冒烟段 T 覆盖类型归一化 + 单动收起 + IO 名称库同刷",
          'Section("段 T' in _sm and "CylinderItem.NormalizeType" in _sm
          and "GenerateIoPoints" in _sm and "IsSingleActing" in _sm)
    check("G29.20 冒烟用旧值「单作用 / 双作用」验证归一化（不是把旧值当类型用）",
          'CylinderItem.NormalizeType("单作用") == "单动气缸"' in _sm
          and 'CylinderItem.NormalizeType("双作用") == "双动气缸"' in _sm
          and 'evm.CylinderSearch = "单动气缸";' in _sm)

# ---------------------------------------------------------------------
section("G30  操作员页：用户切换 / 用户设置（真实 UserStore + 安全 Owner）")
# ---------------------------------------------------------------------
_OPX = r"Views\OperatorPage.xaml"
_OPC = r"Views\OperatorPage.xaml.cs"
_OVM = r"ViewModels\OperatorViewModel.cs"
_SUD = r"Views\SwitchUserDialog.xaml.cs"
_UMD = r"Views\UserManagementDialog.xaml.cs"
_SMK = r".smoke\Program.cs"

_x = read(_OPX)
if _x is None:
    check("G30.0 找到 OperatorPage.xaml", False, _OPX)
else:
    check("G30.0 找到 OperatorPage.xaml", True)
    check("G30.1 工具栏有「切换用户」按钮并接到 SwitchUser_Click",
          '<Button Content="切换用户" Style="{StaticResource TtBaseBtn}" Click="SwitchUser_Click"' in _x)
    check("G30.2 工具栏有「用户设置」按钮并接到 UserSettings_Click",
          '<Button Content="用户设置" Style="{StaticResource TtGrayBtn}" Margin="8,0,0,0" Click="UserSettings_Click"' in _x)
    check("G30.3 当前用户芯片显示 用户名 + 角色（取自真实用户库）",
          'Text="{Binding CurrentUser}"' in _x and 'Text="{Binding CurrentUserRole}"' in _x)
    check("G30.4 假名单下拉已移除（ComboBox 绑 Users / CurrentUser）",
          'ItemsSource="{Binding Users}"' not in _x and 'SelectedItem="{Binding CurrentUser' not in _x)
    check("G30.18 角色标签按 ShowRole 收起（用的是本页自己的 BoolToVis，不引外部资源键）",
          'Visibility="{Binding ShowRole, Converter={StaticResource BoolToVis}}"' in _x)

_c = read(_OPC)
check("G30.5 切换用户：真开 SwitchUserDialog + UserStore.SetCurrent + 刷新状态栏",
      _c is not None
      and 'new SwitchUserDialog()' in _c
      and 'vm.SwitchToUser(name)' in _c
      and 'StatusBarService.RefreshUser();' in _c
      and 'StatusBarService.ReportInfo("已切换用户：" + name);' in _c)
check("G30.6 用户设置：真开 UserManagementDialog，关闭后 RefreshUser 同步",
      _c is not None and 'new UserManagementDialog()' in _c and 'vm.RefreshUser();' in _c)
check("G30.7 两个入口都用安全 Owner（已显示过且不是自己）",
      _c is not None
      and _c.count('if (owner is not null && !ReferenceEquals(owner, dlg) && owner.IsVisible) dlg.Owner = owner;') == 2)

_v = read(_OVM)
if _v is None:
    check("G30.8 OperatorViewModel 用真实用户库", False, _OVM)
else:
    check("G30.8 假名单已清除（操作员 A / 操作 B / 班组长）",
          '操作员 A' not in _v and '操作员 B' not in _v and '班组长' not in _v)
    check("G30.9 当前用户 / 角色直接读 UserStore",
          'public string CurrentUserRole => UserStore.Current?.Role ?? string.Empty;' in _v
          and 'public void RefreshUser() => CurrentUser = UserStore.Current?.Name ?? "未登录";' in _v)
    check("G30.10 SwitchToUser 真调 UserStore.SetCurrent（不再只改一个字符串）",
          'UserStore.SetCurrent(name);' in _v)
    check("G30.11 构造函数里就 RefreshUser（页面一打开就是真实当前用户）",
          'RefreshUser();' in _v)
    check("G30.12 SwitchUserCommand 也走真实切换",
          'SwitchUserCommand = new RelayCommand(p => { if (p is string u) SwitchToUser(u); });' in _v)
    check("G30.17 角色与用户名相同时不重复显示（ShowRole）",
          'public bool ShowRole => !string.Equals(CurrentUserRole, CurrentUser, StringComparison.Ordinal);' in _v)

for _lbl, _f in (("SwitchUserDialog", _SUD), ("UserManagementDialog", _UMD)):
    _d = read(_f)
    check("G30.13 %s：Owner 用安全判空（不再裸赋 Application.Current?.MainWindow）" % _lbl,
          _d is not None
          and 'Owner = Application.Current?.MainWindow;' not in _d
          and 'if (owner is not null && !ReferenceEquals(owner, this) && owner.IsVisible) Owner = owner;' in _d)

_sm = read(_SMK)
if _sm is None:
    check("G30.14 冒烟段覆盖用户切换 / 用户设置", False, _SMK)
else:
    check("G30.14 冒烟段 F 实例化 OperatorPage（XAML 解析）",
          'var op = new NoCodeMotion.Views.OperatorPage();' in _sm)
    check("G30.15 冒烟段 U 覆盖真实切换 + RefreshUser + 按钮在可视化树上",
          'Section("段 U' in _sm and 'uvm.SwitchToUser(probe)' in _sm
          and 'uvm.RefreshUser();' in _sm and 's == "切换用户"' in _sm)
    check("G30.16 冒烟里两个弹窗都要真构造（Owner 陷阱会在这里现形）",
          'new NoCodeMotion.Views.SwitchUserDialog()' in _sm
          and 'new NoCodeMotion.Views.UserManagementDialog()' in _sm)

# ---------------------------------------------------------------------
# G31  节点图画布按键分工：左键选中/框选/拖动、中键平移、滚轮缩放
# ---------------------------------------------------------------------
# 需求：左键在空白处 = 框选，框好后拖任意一个整组一起走；平移让给中键。
# ★ 这里守的全是「静默失效」型：
#   - 平移若回到左键分支里，框选和它会抢同一个手势（表现为拖不出选择框）；
#   - 选择框一旦能吃鼠标，MouseUp 就落在它身上，框选永远收不到结果；
#   - SelectedNode 的 setter 会清 IsSelected，不加组内判断 → 框选高亮一设 SelectedNode 就散架；
#   - 平移基准量错空间（画布带 LayoutTransform）→ 缩放后拖拽速度不对。
_pg = (read(r"Views\NodeGraphPage.xaml.cs") or "").replace("\r\n", "\n")
_pgx = (read(r"Views\NodeGraphPage.xaml") or "").replace("\r\n", "\n")
_ngv = (read(r"ViewModels\NodeGraphViewModel.cs") or "").replace("\r\n", "\n")

_down = _pg[_pg.find("DesignerCanvas_PreviewMouseLeftButtonDown"):
            _pg.find("DesignerCanvas_PreviewMouseDown(object")] if "DesignerCanvas_PreviewMouseLeftButtonDown" in _pg else ""
check("G31.1 左键按下空白处 = 开始框选（该分支里不许再出现 _panning = true）",
      "_marquee = true;" in _down and "_panning = true;" not in _down)
check("G31.2 中键按住才平移，且位移量在父空间（CanvasScroller）上量",
      "if (e.ChangedButton != MouseButton.Middle) return;" in _pg
      and "_panStart = e.GetPosition(CanvasScroller);" in _pg
      and "_panStart = e.GetPosition(DesignerCanvas);" not in _pg)
check("G31.3 选择框挂在画布里且不吃鼠标（否则 MouseUp 落在它身上，框不到节点）",
      'x:Name="MarqueeBox"' in _pgx and 'IsHitTestVisible="False"' in _pgx
      and "MarqueeBox" in _pg)
check("G31.4 框选命中按容器实际渲染范围算（跟着缩放走，不用写死节点宽高）",
      "NodesHost.ItemContainerGenerator.ContainerFromItem(node)" in _pg
      and "el.TransformToVisual(DesignerCanvas)" in _pg)
check("G31.5 没拖动（<4px）的左键单击只取消选中，不产生零宽选择框",
      "Math.Abs(end.X - _marqueeStart.X) > 4" in _pg)
check("G31.6 整组拖动：_groupDrag 分支必须排在 _dragNode 之前（否则只有被点那个动）",
      _pg.find("if (_groupDrag.Count > 0)") < _pg.find("else if (_dragNode != null)")
      and "foreach (var (node, dx, dy) in _groupDrag)" in _pg)
check("G31.7 点中框选组内的节点时不清组（点了就清 = 整组散架）",
      "!(_vm.SelectedNodes.Count > 1 && _vm.SelectedNodes.Contains(anyNode))" in _pg)
check("G31.8 SelectedNode 的 setter 不许抹掉框选组成员的高亮",
      "if (_selectedNode != null && !SelectedNodes.Contains(_selectedNode)) _selectedNode.IsSelected = false;" in _ngv)
check("G31.9 删除支持整组（按 id 集合连着连线一起删）",
      "var ids = new System.Collections.Generic.HashSet<string>(group.Select(n => n.Id));" in _ngv
      and "ids.Contains(c.SourceId) || ids.Contains(c.TargetId)" in _ngv)
check("G31.10 右下角小字提示存在且不挡鼠标（画布按键分工就靠它告诉用户）",
      "中键按住拖拽平移" in _pgx and "左键在空白处框选" in _pgx
      and _pgx.count('IsHitTestVisible="False"') >= 2)

# ---------------------------------------------------------------------
# G32  流程算子白话名：界面存显示名，引擎认内部名，只在边界翻译一次
# ---------------------------------------------------------------------
# 需求：流程里的算子/功能/运算名称要让没经验的人看得懂（绝对移动→走到位置、
#       二值化→分成黑白两块、modbus→外部通讯…），且老工程打开自动转换、旧写法继续能跑。
# ★ 这里守的是「改名改一半」的静默失效：
#   下拉换了词但执行处没归一 → 步骤静默走 default 分支（不报错，只是不动）；
#   归一表被后写的同显示名覆盖 → 「设为」翻成「修改」；
#   气缸的「复位」被统一翻成「关闭」→ 界面显示错、语义也错。
_fv = (read(r"Services\FlowVocab.cs") or "").replace("\r\n", "\n")
if not _fv:
    check("G32 找到 FlowVocab.cs", False, r"Services\FlowVocab.cs 缺失")
else:
    check("G32.1 词表三张（功能 / 运算 / 视觉算子）都在，且候选数组也在这里（唯一来源）",
          all(k in _fv for k in ("FunctionTable", "OperationTable", "PreOpTable",
                                 "FunctionChoices", "MoveOps", "IoWriteOps", "CylOps", "PreOpChoices")))
    check("G32.2 「复位」按上下文分流：气缸保持复位、输出点显示关闭（早返回，不再查表）",
          'if (string.Equals(s, "复位", StringComparison.Ordinal)) return IsCylinder(f) ? "复位" : "关闭";' in _fv)
    check("G32.3 显示名→内部名 取表里第一个（防「设为」被后面的「修改」盖掉）",
          "if (!d.ContainsKey(display)) d[display] = internalName;" in _fv)
    check("G32.4 迁移按单个流程复用（Migrate 走 MigrateFlow，AI 生成与打开工程同一条路）",
          "touched += MigrateFlow(flow);" in _fv and "public static int MigrateFlow(FlowItem? flow)" in _fv)

    # 执行边界：谁读 Function / Operation / PreOp，谁就必须先过词表
    _bnd = {
        r"ViewModels\FlowRunnerService.cs": "FlowVocab.ToInternalFunction(s.Function)",
        r"Services\SimFlowPlayer.cs": "FlowVocab.ToInternalFunction(s.Function)",
        r"ViewModels\FlowViewModel.cs": "FlowVocab.ToInternalFunction(step.Function)",
        r"Services\Vision\VisionEngine.cs": "FlowVocab.ToInternalPreOp(s.PreOp)",
        r"Services\NgRunner.cs": "FlowVocab.ToInternalOperation(opShown",
        r"Services\NgConditionEvaluator.cs": "FlowVocab.ToInternalFunction(Prop(n",
    }
    for _rel, _call in _bnd.items():
        _src = (read(_rel) or "").replace("\r\n", "\n")
        check("G32.5 [%s] 读用户存的名字处过了词表" % _rel.split("\\")[-1],
              _call in _src, "缺 " + _call)

    # 视觉报告要显示用户挑的那个名字，不是内部算子名
    _ve = (read(r"Services\Vision\VisionEngine.cs") or "").replace("\r\n", "\n")
    check("G32.6 视觉报告里的算子名用界面显示的那个（FinalizePre 传 s.PreOp）",
          "FinalizePre(o, s, s.PreOp, report, progress)" in _ve
          and 'FinalizePre(o, s, "二值化"' not in _ve)

    # 下拉候选与词表对齐：两处各写一份迟早对不上（下拉出现候选里没有的选中值 = 显示空白）
    def _csharp_array(name):
        i = _fv.find("string[] " + name)
        if i < 0: return []
        j = _fv.find(";", i)
        body = _fv[_fv.find("{", i):j]
        return [x for x in re.findall(r'"([^"]+)"', body)]

    _fx = (read(r"Views\FlowPage.xaml") or "").replace("\r\n", "\n")
    _i = _fx.find('x:Key="FunctionOptions"')
    _xaml_funcs = re.findall(r"<sys:String>([^<]+)</sys:String>",
                             _fx[_i:_fx.find("</x:Array>", _i)]) if _i >= 0 else []
    check("G32.7 流程页「功能」候选 = FlowVocab.FunctionChoices（一处改两处生效）",
          _xaml_funcs == _csharp_array("FunctionChoices"),
          "xaml=" + ",".join(_xaml_funcs) + " | vocab=" + ",".join(_csharp_array("FunctionChoices")))

    _vfx = (read(r"Views\VisualFlowPage.xaml") or "").replace("\r\n", "\n")
    _j = _vfx.find('x:Key="PreOps"')
    _xaml_pre = re.findall(r"<sys:String>([^<]+)</sys:String>",
                           _vfx[_j:_vfx.find("</x:Array>", _j)]) if _j >= 0 else []
    _voc_pre = _csharp_array("PreOpChoices")
    check("G32.8 视觉页算子候选全部来自词表（不允许出现词表外的算子名）",
          _xaml_pre and all(x in _voc_pre for x in _xaml_pre),
          "越界: " + ",".join(x for x in _xaml_pre if x not in _voc_pre))

    # 旧内部名不许再出现在候选里（否则等于没改）
    _stale = [w for w in ("modbus", "绝对移动", "相对移动", "置位", "取模", "二值化", "Sobel边缘", "ROI裁剪")
              if w in _xaml_funcs or w in _xaml_pre]
    check("G32.9 下拉候选里没有残留的内部术语", not _stale, "残留: " + ",".join(_stale))

    # 两条入口都要迁移：打开工程 + AI 粘贴生成
    check("G32.10 打开工程会迁移老存值（ProjectData 载入路径调 FlowVocab.Migrate）",
          "Services.FlowVocab.Migrate(this);" in (read(r"Models\ProjectData.cs") or ""))
    check("G32.11 AI 粘贴生成后也迁移（否则下拉会出现候选里没有的选中值，显示成空白）",
          "FlowVocab.MigrateFlow(f);" in (read(r"Services\AiProjectExchange.cs") or ""))

    # 给 AI 的提示词要报白话名（AI 照旧写内部名也认，见 ClampTo 的词表容错）
    _ai = (read(r"Services\AiProjectExchange.cs") or "").replace("\r\n", "\n")
    check("G32.12 提示词里的「功能」候选取自词表，不另写一份",
          'string.Join(" / ", FlowVocab.FunctionChoices)' in _ai)
    check("G32.13 AI 写的旧内部名也会被认（ClampTo / ClampOperation 先过词表再判合法）",
          "FlowVocab.ToInternalFunction(s)" in _ai and "FlowVocab.ToInternalOperation(given, func)" in _ai)

# ---------------------------------------------------------------------
# G33  视觉算子：目录（白话名）↔ 实现 ↔ 自动参数界面
# ---------------------------------------------------------------------
# 需求：算子要对齐 LV Studio / VisionMaster 的数量，名字要白话，实现要真做（不是占位）。
# ★ 这里守的三条最容易烂掉的地方：
#   ① 目录里列了算子但执行器没实现 → 现场点了只得到「未实现」，比不列更坑；
#   ② 引擎仍先按 StepType 分派 → 算子永远跑不到（不报错，只是没结果）；
#   ③ 参数界面退回手写 → 加一个算子就要改一遍 XAML，迟早和目录对不上。
import re as _re33

_vop = (read(r"Services\Vision\VisionOperators.cs") or "").replace("\r\n", "\n")
_vrun = (read(r"Services\Vision\VisionOpRunner.cs") or "").replace("\r\n", "\n")
_veng = (read(r"Services\Vision\VisionEngine.cs") or "").replace("\r\n", "\n")
_vxaml = (read(r"Views\VisualFlowPage.xaml") or "").replace("\r\n", "\n")
_vvm = (read(r"Views\VisualFlowDetailViewModel.cs") or "").replace("\r\n", "\n")
_vstep = (read(r"Models\VisualFlowStep.cs") or "").replace("\r\n", "\n")

if not _vop or not _vrun:
    check("G33 找到视觉算子目录 / 执行器", False, "VisionOperators.cs 或 VisionOpRunner.cs 缺失")
else:
    _keys = _re33.findall(r'Key\s*=\s*"([a-z][a-z0-9_]*)"', _vop)
    _impl = _re33.findall(r'case\s+"([a-z][a-z0-9_]*)"\s*:', _vrun)
    check("G33.1 目录里的每个算子在执行器里都有实现（列了没实现 = 现场点了报未实现）",
          _keys and not [k for k in _keys if k not in _impl],
          "缺实现: " + ",".join(k for k in _keys if k not in _impl))
    check("G33.2 算子键不重复", len(_keys) == len(set(_keys)), ",".join(_keys))
    # 逐项数：每个算子都该有一行 Category / Summary / Outputs（"Key =" 在参数定义里也出现，不能拿来计数）
    _n = len(_keys)
    check("G33.3 每个算子都有白话名 / 分类 / 一句话说明 / 输出清单",
          _vop.count('Name = "') == _n and _vop.count("Category = \"") == _n
          and _vop.count("Summary = \"") == _n and _vop.count("Outputs = \"") == _n,
          "算子 %d 个，但 Name/Category/Summary/Outputs 分别是 %d/%d/%d/%d" % (
              _n, _vop.count('Name = "'), _vop.count("Category = \""),
              _vop.count("Summary = \""), _vop.count("Outputs = \"")))
    check("G33.4 引擎在按「步骤类型」分派之前先看算子（否则算子永远跑不到）",
          _veng.find("VisionOperators.Find(s.Op)") < _veng.find('switch ((s.StepType ?? "").Trim())'))
    check("G33.5 算子内部异常兜成「这一步失败」，不打断整条流程",
          "catch (Exception ex)" in _vrun and "这个算子跑不起来：" in _vrun)
    check("G33.6 参数界面由目录自动生成（不是为每个算子手写一遍 XAML）",
          'ItemsSource="{Binding OpParams}"' in _vxaml and "VisionParamEditor" in _vvm)
    check("G33.7 参数包用 JsonDocument 解析（AI/人手写数字值不会被整体丢掉）",
          "JsonDocument.Parse(_paramJson)" in _vstep and "Dictionary<string, string>" in _vstep)
    check("G33.8 算子输出量是结构化的（Numbers），不靠从 Summary 里正则抽数",
          "public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, double>> Numbers" in _veng
          and "Numbers.Add(" in _vrun)
    check("G33.9 步骤类型里选到算子 → 写 Op 且类型显示算子名；选回基础类型 → 清空 Op（两套参数卡不同时冒出来）",
          'SelectedStep.StepType = spec.Name;' in _vvm and 'SelectedStep.Op = "";' in _vvm
          and "VisionOperators.ByCategory(_selectedGroup)" in _vvm)

# ---------------------------------------------------------------------
# G34  节点工具箱：算子要看得见、拖得动、跑得起来
# ---------------------------------------------------------------------
# 需求（2026-10-21 现场反馈）：节点图工具箱里找不到算子，而且列表被截断又滚不动。
#   ① 工具箱必须能滚动 —— 这条就是那个 bug 的回归网（当年写死 Disabled，编译运行都不报错）；
#   ② 算子芯片和它的参数表都必须来自 VisionOperators 目录，不另写一份清单；
#   ③ 老工程载入不能把已填的算子参数抹掉，换了算子参数表要整张跟着换；
#   ④ 卡片标题跟着所选算子变 —— 41 个节点都叫「视觉算子」等于没接。
_ngxaml = (read(r"Views\NodeGraphPage.xaml") or "").replace("\r\n", "\n")
_ngpage = (read(r"Views\NodeGraphPage.xaml.cs") or "").replace("\r\n", "\n")
_ngvm = (read(r"ViewModels\NodeGraphViewModel.cs") or "").replace("\r\n", "\n")
_ngnvm = (read(r"ViewModels\NodeGraphNodeViewModel.cs") or "").replace("\r\n", "\n")
_ngdefs = (read(r"Models\NodeGraph\NgDefs.cs") or "").replace("\r\n", "\n")
_ngmodel = (read(r"Models\NodeGraph\NgModel.cs") or "").replace("\r\n", "\n")
_ngrun = (read(r"Services\NgRunner.cs") or "").replace("\r\n", "\n")
_ngvis = (read(r"Services\Vision\NgVisionExecutor.cs") or "").replace("\r\n", "\n")
_vfpage = (read(r"Views\VisualFlowPage.xaml") or "").replace("\r\n", "\n")
_vfcode = (read(r"Views\VisualFlowPage.xaml.cs") or "").replace("\r\n", "\n")
_vfvm = (read(r"Views\VisualFlowDetailViewModel.cs") or "").replace("\r\n", "\n")

if not _ngxaml or not _ngdefs:
    check("G34 找到节点图页面 / 节点定义", False, "NodeGraphPage.xaml 或 NgDefs.cs 缺失")
else:
    check("G34.1 工具箱能滚动（曾经写死 Disabled，通讯组被截在屏幕外）",
          'VerticalScrollBarVisibility="Auto"' in _ngxaml and 'ItemsSource="{Binding ToolboxGroups}"' in _ngxaml)
    check("G34.2 工具箱里的算子芯片来自目录（不另写一份清单）",
          "VisionOperators.All2" in _ngdefs and "NgNodeDefinitions.OpTools" in _ngvm
          and 'Title = "算子 · "' in _ngvm)
    check("G34.3 算子节点的参数表来自目录（bool 参数渲成 是|否 下拉）",
          "foreach (var pd in spec.Params)" in _ngdefs and '"是|否"' in _ngdefs)
    check("G34.4 老工程载入不抹掉算子参数（Normalize 对 VisionOp 走 OpProps）",
          "n.Kind == NgKind.VisionOp ? NgNodeDefinitions.OpProps" in _ngmodel)
    check("G34.5 换了算子，参数表整张跟着换（不是只改一个下拉值）",
          "RebuildOpProps()" in _ngnvm and 'propName == "算子"' in _ngnvm)
    check("G34.6 卡片标题 / 颜色跟着所选算子走",
          "NgNodeDefinitions.TitleOf(_model)" in _ngnvm and "NgNodeDefinitions.ColorOf(_model)" in _ngnvm)
    check("G34.7 算子节点真的会被执行（runner 认它 + 执行器有 Op 分支）",
          "case NgKind.VisionOp:" in _ngrun and "NgKind.VisionOp => Op(node)" in _ngvis)
    check("G34.8 算子给的数写成变量，下游节点能引用（按变量前缀拼名）",
          "_setVar(prefix + kv.Key, kv.Value)" in _ngvis and 'Prop(node, "变量前缀"' in _ngvis)
    check("G34.9 拖拽带的是整个条目（只带 NgKind 会丢算子键 → 参数表铺不出来）",
          'new DataObject("NgTool", tool)' in _ngpage and "_vm.AddTool(tool" in _ngpage
          and 'CommandParameter="{Binding}"' in _ngxaml)
    check("G34.10 视觉流程点「添加步骤」能直接按分类挑算子（不是先加通用步骤再去长下拉里找）",
          'Click="AddStep_Click"' in (_vfpage or "")
          and "VisionOperators.Categories" in (_vfcode or "")
          and "AddOperatorStep" in (_vfvm or "")
          and "s.Count == 0" in (_vfvm or ""))
    check("G34.11 视觉流程「步骤类型」是两级：上面选分组、下面选类型，算子并进类型下拉（不再单独一个算子下拉）",
          "GroupChoices" in (_vfvm or "") and "SelectedGroup" in (_vfvm or "")
          and "StepTypeChoices" in (_vfvm or "")
          and 'ItemsSource="{Binding StepTypeChoices}"' in (_vfpage or "")
          and 'ItemsSource="{Binding GroupChoices}"' in (_vfpage or "")
          and 'ItemsSource="{Binding OpChoices}"' not in (_vfpage or "")
          and (_vfpage or "").find('ItemsSource="{Binding GroupChoices}"')
              < (_vfpage or "").find('ItemsSource="{Binding StepTypeChoices}"'))

check("G34.12 节点工具箱里算子分组排在最下面（现场先摆流程骨架，再往下挑算子）",
      "NgDomain.Logic, NgDomain.VisionOp };" in _ngdefs)

# ---------------------------------------------------------------------
# G35  LV Studio 运控算子（手册 6.1 运动控制）在节点图里的落地
# ---------------------------------------------------------------------
# 手册 6.1 共 8 个工具：单轴运动 / 回零 / Jog / 轴停止 / 直线插补 / 圆弧插补 / 点表运动 / IO 控制。
# 已有单轴-回零-点表-IO，本次补 Jog / 轴停止 / 伺服使能 / 直线插补 / 三点圆弧插补。
_ngdefs35 = (read(r"Models\NodeGraph\NgDefs.cs") or "").replace("\r\n", "\n")
_ngrun35 = (read(r"Services\NgRunner.cs") or "").replace("\r\n", "\n")
_bridge35 = (read(r"Services\Hardware\Cards\WenQiZhiCardBridge.cs") or "").replace("\r\n", "\n")
_iface35 = (read(r"Services\IHardwareBridge.cs") or "").replace("\r\n", "\n")

check("G35.1 LV 手册 6.1 的 8 个运控工具在节点图里都有对应节点",
      all(("NgKind." + k) in _ngdefs35 for k in
          ["MoveAxis", "Home", "AxisJog", "AxisStop", "LineInterp", "ArcInterp", "PointGo", "IoWrite"]))
check("G35.2 新增运控节点在执行器里都有 case（列了不实现 = 点了没反应）",
      all(("case NgKind." + k + ":") in _ngrun35 for k in
          ["AxisJog", "AxisStop", "AxisServo", "LineInterp", "ArcInterp"]))
check("G35.3 插补走卡族接口，卡不支持时给人话原因（不静默假装走了）",
      "CardAxesLineUnit(line)" in _bridge35 and "CardAxesArcMoveCenterUnit(arc)" in _bridge35
      and "这张卡可能不支持插补" in _bridge35 and "string LineInterpolate(" in _iface35)
check("G35.4 单轴运动补齐 LV 的「等待到位 / 到位延时」",
      'Name = "等待到位"' in _ngdefs35 and 'Name = "到位延时ms"' in _ngdefs35
      and 'GetProp(node, "等待到位", "否")' in _ngrun35)
check("G35.5 卡层插补先过软限位（不能绕过限位撞机）",
      "CheckSoftLimit(target.Axis, target.Target);" in _bridge35 and "CheckSoftLimit(ax1, end1);" in _bridge35)
check("G35.6 直线插补要单独写速度曲线（卡族 CardAxesLineUnit 不读速度，漏了这步速度参数等于白填）",
      "impl.CardAxisLineUnitSpeed(line)" in _bridge35
      and _bridge35.find("impl.CardAxisLineUnitSpeed(line)") < _bridge35.find("impl.CardAxesLineUnit(line)"))

# ---------------------------------------------------------------------
# G36  节点参数不许是摆设（定义了却没人读 = 用户填了不生效）
# ---------------------------------------------------------------------
# 背景：2026-10-21 审计发现「电机走位」的加速度、回零的「方向/模式」、等待输入的「超时ms」
# 都只是摆在界面上好看 —— 执行器根本没读。这类假参数比没参数更坑（现场会以为已经设了）。
_ngdefs36 = (read(r"Models\NodeGraph\NgDefs.cs") or "").replace("\r\n", "\n")
_ngrun36 = (read(r"Services\NgRunner.cs") or "").replace("\r\n", "\n")
_ngvis36 = (read(r"Services\Vision\NgVisionExecutor.cs") or "").replace("\r\n", "\n")

_switch_start = _ngrun36.find("switch (node.Kind)")
_switch_end = _ngrun36.find("// 视觉节点", _switch_start)
_switch_body = _ngrun36[_switch_start:_switch_end] if _switch_start >= 0 else ""
_case_starts = [(m.group(1), m.start(), m.end())
                for m in _re33.finditer(r"case NgKind\.(\w+):", _switch_body)]
_def_starts = [(m.group(1), m.start()) for m in _re33.finditer(r"\[NgKind\.(\w+)\] = new\(\)", _ngdefs36)]
_defs36 = {}
for _i, (_k, _pos) in enumerate(_def_starts):
    _end = _def_starts[_i + 1][1] if _i + 1 < len(_def_starts) else _ngdefs36.find("    };", _pos)
    _defs36[_k] = _re33.findall(r'(?<![A-Za-z])Name = "([^"]+)"', _ngdefs36[_pos:_end])

_skip36 = {"Start", "End", "ParallelFork", "ParallelJoin", "Decision", "Loop", "Compute"}
# 这几个节点由视觉执行器读参数（不在 NgRunner 的 switch 里）
_vision36 = {"CamCapture", "TemplateMatch", "DefectDetect", "Measure", "Align", "Calib"}
_fake36 = []
for _i, (_k, _s0, _e0) in enumerate(_case_starts):
    _end = _case_starts[_i + 1][1] if _i + 1 < len(_case_starts) else len(_switch_body)
    _src = _switch_body[_e0:_end]
    if _k in _vision36:
        _src = _ngvis36
    elif _k == "VisionOp":
        continue          # 参数表来自算子目录，逐项执行见 G33
    elif _k in _skip36:
        continue
    _used = set(_re33.findall(r'(?:Get(?:Prop|DoubleProp|IntProp)|Prop|Num)\(node, "([^"]+)"', _src))
    _unused = [p for p in _defs36.get(_k, []) if p not in _used]
    if _unused:
        _fake36.append("%s: %s" % (_k, ",".join(_unused)))
check("G36.1 节点上定义的每个参数执行器都真的读（假参数比没参数更坑）",
      not _fake36, "; ".join(_fake36))

_ngnvm36 = (read(r"ViewModels\NodeGraphNodeViewModel.cs") or "").replace("\r\n", "\n")
check("G36.2 名称类参数给的是工程里的真实候选（轴1/轴2、设备、点位表、相机）",
      "char.IsDigit(Name[1])" in _ngnvm36 and '"设备" => Catalog.CommNames' in _ngnvm36
      and '"点位表" => Catalog.PointTableNames' in _ngnvm36
      and '"相机" => Catalog.CameraNames' in _ngnvm36)
check("G36.3 点位表名有单独的候选源（SetPointTable 由点位页同步）",
      "PointTableNames" in (read(r"Services\Catalog.cs") or "")
      and "Catalog.SetPointTable(" in (read(r"ViewModels\PointViewModel.cs") or ""))
check("G36.4 每个节点都有标准名 + 详细说明（工具箱悬停不能只显示标题）",
      'StdName = "' in _ngdefs36 and 'public string Tip' in _ngdefs36
      and _ngdefs36.count('Summary = "') >= 32)
check("G36.5 悬停提示带参数清单（Tip 自己拼 Props，不用每页手写）",
      'Append("参数：")' in _ngdefs36)
check("G36.6 工具箱按钮绑的是 Tip（绑 Title 就只有一行字）",
      'ToolTip="{Binding Tip}"' in _ngxaml)

# ---------------------------------------------------------------------
# G37  计时精度：Sleep(1) 在 Windows 上默认实际睡 ~15ms
# ---------------------------------------------------------------------
_timing37 = (read(r"Services\Timing.cs") or "").replace("\r\n", "\n")
_app37 = (read(r"App.xaml.cs") or "").replace("\r\n", "\n")
_runner37 = (read(r"Services\NgRunner.cs") or "").replace("\r\n", "\n")
_bridge37 = (read(r"Services\Hardware\Cards\WenQiZhiCardBridge.cs") or "").replace("\r\n", "\n")

check("G37.1 启动时把进程计时精度提到 1ms、退出还原（默认 Sleep(1) 实测 ~15ms）",
      "timeBeginPeriod" in _timing37 and "Timing.Raise(1);" in _app37 and "Timing.Release();" in _app37)
check("G37.2 等待热路径用 Timing.Sleep（等轴到位 / 节点延时 / 到位延时）",
      "Timing.Sleep(slice)" in _bridge37 and "Timing.Sleep(slice)" in _runner37
      and "Timing.Sleep(settle)" in _runner37)

# ---------------------------------------------------------------------
# G38  输出目录不再被原生 dll 挤满：原生统一进 native\，托管留 exe 同级
# ---------------------------------------------------------------------
# 2026-10-21 用户反馈「这么多 dll，都放在一个文件夹吧」。
#   · 原生 dll 可以挪（启动时 SetDllDirectory 把 native\ 加进搜索路径）；
#   · 托管程序集（含 C++/CLI 的 *_CLRLib）必须与 exe 同级 —— .NET 只从那里 + deps.json 找。
_csproj38 = (read(r"NoCodeMotion.csproj") or "").replace("\r\n", "\n")
_app38 = (read(r"App.xaml.cs") or "").replace("\r\n", "\n")
_native38 = ["LTDMC.dll", "MCN420.dll", "OpenCvSharpExtern.dll", "opencv_world480.dll",
             "GrayModelNative.dll", "vcomp140.dll", "sldmv.dll"]

check("G38.1 原生 dll 全部落到输出目录的 native\\ 子目录",
      all(('Link="native\\%s"' % d) in _csproj38 for d in _native38)
      and all(('Link="%s"' % d) not in _csproj38 for d in _native38))
check("G38.2 启动时把 native\\ 加进 DLL 搜索路径（否则 P/Invoke 找不到）",
      'SetDllDirectory(nativeDir)' in _app38
      and 'Path.Combine(System.AppContext.BaseDirectory, "native")' in _app38)
_managed38 = ["WMX3Api_CLRLib.dll", "RtMotionApiNET.dll", "EcApi_CLRLib.dll",
              "CoreMotionApi_CLRLib.dll", "IOApi_CLRLib.dll", "EventApi_CLRLib.dll"]
check("G38.3 托管卡库的输出落点仍是 exe 同级（TargetPath 不带 native\\）",
      all(('TargetPath="%s"' % d) in _csproj38 for d in _managed38)
      and all(('TargetPath="native\\%s"' % d) not in _csproj38 for d in _managed38)
      and 'Link="MvCameraControl.Net.dll"' not in _csproj38)
check("G38.4 托管卡库在工程树里归到 Native\\ 下（Link 与 TargetPath 分离，树整洁但运行时位置不变）",
      all(('Link="Native\\%s" TargetPath="%s"' % (d, d)) in _csproj38 for d in _managed38))
check("G38.5 3D 模型放在 Models\\CAD\\（仓库根不再放 47MB 的 stp）",
      'Content Include="Models\\CAD\\IR-R10-140S-INT-3D-3D.stp"' in _csproj38
      and 'Content Include="IR-R10-140S-INT-3D-3D.stp"' not in _csproj38)

# ---------------------------------------------------------------------
# G39  参数说明直接显示（不靠悬停），且每个参数都有说明
# ---------------------------------------------------------------------
# 用户 2026-10-21：「不需要悬停，直接显示大白话」——没经验的人不会去猜哪里能悬停。
_ngdefs39 = (read(r"Models\NodeGraph\NgDefs.cs") or "").replace("\r\n", "\n")
_ngxaml39 = (read(r"Views\NodeGraphPage.xaml") or "").replace("\r\n", "\n")
_vfxaml39 = (read(r"Views\VisualFlowPage.xaml") or "").replace("\r\n", "\n")
_manual39 = (read(r"Views\OperatorManualPage.xaml.cs") or "").replace("\r\n", "\n")

check("G39.1 节点参数说明直接显示在参数行下面（不是 ToolTip）",
      'Text="{Binding Help}"' in _ngxaml39 and 'ToolTip="{Binding Help}"' not in _ngxaml39)
check("G39.2 视觉流程的算子参数说明也直接显示",
      'Text="{Binding Help}"' in _vfxaml39 and 'ToolTip="{Binding Help}"' not in _vfxaml39)
check("G39.3 每个节点参数都写了说明（Help 不空）",
      'public string Help { get; init; } = "";' in _ngdefs39
      and _ngdefs39.count('Help = "') >= 95)
check("G39.4 说明书有「名词解释」章节（Jog / 使能 / 回零 / 脉冲当量 这类词查得到）",
      "名词解释" in _manual39 and "脉冲当量" in _manual39 and "点动（Jog）" in _manual39)

print(f"\n====================  {npass} PASS / {nfail} FAIL  ====================")
if fails:
    print("失败清单：")
    for l in fails:
        print(l)
sys.exit(1 if nfail else 0)
