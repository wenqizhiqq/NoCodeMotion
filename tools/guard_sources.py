# -*- coding: utf-8 -*-
r"""源码文本回归守卫 —— 伺服使能极性 / Jog 失败提示 / 加减速语义 / 暂停停止守卫 / 多开退出 / 图形生成 / IO 虚拟化。

为什么单独一个脚本：C# 冒烟程序从 %TEMP% 启动，读 D:\ 下的源码会被沙箱替换成无效内容
（实测读到 237036 字节乱码，真实文件 232940 字节）。本脚本从仓库目录运行，读到的才是真文件。

★ 2026-10-07：本脚本原在 %TEMP%\ncm_patch\，被系统临时目录清理删掉过一次 →
  改为放仓库内 tools\（持久）。用法（必须在仓库根目录跑）：
    C:/Users/admin/.workbuddy-ai/binaries/python/versions/3.13.12/python.exe -u tools/guard_sources.py
"""
import os
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
check("G5.5 csproj 已声明随程序复制",
      csproj is not None and
      '<None Include="Native\\Mcn420ErrorCode.xml" Link="Mcn420ErrorCode.xml" CopyToOutputDirectory="PreserveNewest" />' in csproj)

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
    check("G7.8 轴自动生成：名称「轴0..轴N-1」= 轴号（MakeAxis(tag,s,s)）",
          "data.Axes.Add(MakeAxis(tag, s, s));" in avm)
    check("G7.8b 轴生成循环从 0 起（for s=0; s<count）",
          "for (int s = 0; s < count; s++)" in avm)
    check("G7.9 IO 自动生成：主板位号从 0 起（MakeIo(..., 0, s)）",
          'data.Inputs.Add(MakeIo(tag, "输入", nIn++, 0, s));' in avm)
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
    check("G12.12 轴页两个字段带了说明 ToolTip（写明单位是秒、常用 0.1~0.5）",
          g12_xaml is not None
          and "ToolTip" in g12_xaml
          and "常用 0.1~0.5 秒" in g12_xaml)

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
    check("G22.8 新建工程弹窗提示了「工位-对象」命名约定与分组前缀规则",
          "工位-对象" in _np and "第一个「-」之前的文字就是下拉框的分组前缀" in _np)

for _rel, _tag in [(r"Views\AxisPage.xaml", "轴"),
                   (r"Views\IoPage.xaml", "IO"),
                   (r"Views\CylinderPage.xaml", "气缸"),
                   (r"Views\VariablePage.xaml", "变量")]:
    _pg = read(_rel)
    if _pg is None:
        check(f"G22.9 [{_tag}] 页面存在并挂了命名约定提示栏", False, "文件缺失")
        continue
    _x = _pg.replace("\r\n", "\n")
    check(f"G22.9 [{_tag}] 页挂了 PageHintBar 命名约定提示（操作 + 注意两行都在）",
          "<local:PageHintBar" in _x
          and "OperationText=" in _x and "PrecautionText=" in _x
          and "工位-对象" in _x and "分组前缀" in _x)

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
    check("G23.2 视觉流程页工具类型含「字符识别」", "<sys:String>字符识别</sys:String>" in _x)
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
          '+ $"|{cfg.SecsRole}|{cfg.SecsDeviceId}";' in _c28)

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
          'Text="SECS / HSMS 参数（SEMI E37 会话 + E5 报文）"' in _x28
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

print(f"\n====================  {npass} PASS / {nfail} FAIL  ====================")
if fails:
    print("失败清单：")
    for l in fails:
        print(l)
sys.exit(1 if nfail else 0)
