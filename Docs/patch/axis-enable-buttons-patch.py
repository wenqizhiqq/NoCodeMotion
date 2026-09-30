# -*- coding: utf-8 -*-
"""
NoCodeMotion - 轴页面「使能 / 不使能」按钮改造补丁

改动内容（7 个文件）：
  1. Views/AxisPage.xaml
       - 删除「基本信息」卡片里的「上电使能」开关（AppleToggle）
       - 「轴控制」按钮区：[使能][不使能] 一行，[停止] 单独一行
  2. ViewModels/AxisRowViewModel.cs                    新增 DisableCommand
  3. Services/Hardware/AxisMonitor.cs                  新增 AxisMonitorService.Disable
  4. Services/IHardwareBridge.cs                       接口新增 DisableAxis
  5. Services/StubHardwareBridge.cs                    桩实现 DisableAxis
  6. Services/Hardware/Cards/WenQiZhiCardBridge.cs     卡族实现 DisableAxis
  7. Services/Hardware/Leadshine/LeadshineHardwareBridge.cs  雷赛实现 DisableAxis

安全策略：
  * 原文件先整份备份到 _backup/axis-enable-<时间戳>/（保留相对目录结构）
  * 每处替换必须精确命中 1 次；任何一处命中 0 次或多次，立刻中止且不写任何文件
  * 读写按文件自身编码（UTF-8 BOM / UTF-8 / GB18030）与行尾（CRLF / LF）原样往返

用法：
  python docs\\patch\\axis-enable-buttons-patch.py            # 打补丁 + 自动 dotnet build
  python docs\\patch\\axis-enable-buttons-patch.py --dry-run   # 只校验锚点，不写文件
  python docs\\patch\\axis-enable-buttons-patch.py --no-build  # 打补丁但不构建
"""

import os
import shutil
import subprocess
import sys
import time

ROOT = r"D:\wqz\code\NoCodeMotion"

DRY = "--dry-run" in sys.argv
DO_BUILD = "--no-build" not in sys.argv

# ---------------------------------------------------------------- 补丁清单
# (相对路径, 说明标签, 原始片段, 替换为)   片段里的 \n 会按目标文件的实际行尾还原
PATCHES = []

# ============ 1. AxisPage.xaml：删掉「上电使能」开关 ============
PATCHES.append((
    r"Views\AxisPage.xaml",
    "remove 上电使能 toggle",
    """                                <Rectangle Style="{StaticResource AppleHairline}"/>
                                <DockPanel Style="{StaticResource AppleRow}">
                                    <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="上电使能"/>
                                    <CheckBox DockPanel.Dock="Right" Style="{StaticResource AppleToggle}" IsChecked="{Binding SelectedItem.Enabled}" ToolTip="工程配置项：勾选后，连接控制器 / 改动该轴时会自动下发「使能」。手动使能请用右侧「轴控制」里的「使能」按钮。"/>
                                </DockPanel>
""",
    "",
))

# ============ 2. AxisPage.xaml：轴控制按钮区 ============
PATCHES.append((
    r"Views\AxisPage.xaml",
    "axis-control buttons: +不使能, 停止 own row",
    """                                    <UniformGrid Columns="2" Margin="0,0,0,6">
                                        <Button Content="使能" Style="{StaticResource TtPillBlueBtn}" Margin="0,0,3,0"
                                                Command="{Binding Monitor.EnableCommand}" ToolTip="给该轴使能（上伺服）。未使能的轴收下指令也不会动。"/>
                                        <Button Content="停止" Style="{StaticResource TtPillRedBtn}" Margin="3,0,0,0"
                                                Command="{Binding Monitor.StopCommand}" ToolTip="减速停止该轴（Jog 也用它停）"/>
                                    </UniformGrid>
""",
    """                                    <UniformGrid Columns="2" Margin="0,0,0,6">
                                        <Button Content="使能" Style="{StaticResource TtPillBlueBtn}" Margin="0,0,3,0"
                                                Command="{Binding Monitor.EnableCommand}" ToolTip="给该轴使能（上伺服）。未使能的轴收下指令也不会动。"/>
                                        <Button Content="不使能" Style="{StaticResource TtPillGrayBtn}" Margin="3,0,0,0"
                                                Command="{Binding Monitor.DisableCommand}" ToolTip="解除使能（下伺服）。轴会失去保持力矩，请先确认轴已停止、无悬吊负载。"/>
                                    </UniformGrid>
                                    <Button Content="停止" Style="{StaticResource TtPillRedBtn}" Margin="0,0,0,6"
                                            Command="{Binding Monitor.StopCommand}" ToolTip="减速停止该轴（Jog 也用它停）"/>
""",
))

# ============ 3. AxisRowViewModel.cs：DisableCommand ============
PATCHES.append((
    r"ViewModels\AxisRowViewModel.cs",
    "ctor: create DisableCommand",
    """            EnableCommand = new RelayCommand(_ => Invoke("使能", () => AxisMonitorService.Enable(_item)));
""",
    """            EnableCommand = new RelayCommand(_ => Invoke("使能", () => AxisMonitorService.Enable(_item)));
            DisableCommand = new RelayCommand(_ => Invoke("解除使能", () => AxisMonitorService.Disable(_item)));
""",
))

PATCHES.append((
    r"ViewModels\AxisRowViewModel.cs",
    "property: DisableCommand",
    """        public ICommand EnableCommand { get; }
        public ICommand StopCommand { get; }
""",
    """        public ICommand EnableCommand { get; }
        public ICommand DisableCommand { get; }
        public ICommand StopCommand { get; }
""",
))

# ============ 4. AxisMonitor.cs：Disable ============
PATCHES.append((
    r"Services\Hardware\AxisMonitor.cs",
    "AxisMonitorService.Disable",
    """        /// <summary>使能轴（真实下发）。</summary>
        public static void Enable(AxisItem axis) => HardwareBridge.Current?.EnableAxis(axis);
""",
    """        /// <summary>使能轴（真实下发）。</summary>
        public static void Enable(AxisItem axis) => HardwareBridge.Current?.EnableAxis(axis);

        /// <summary>解除使能（下伺服，真实下发）。</summary>
        public static void Disable(AxisItem axis) => HardwareBridge.Current?.DisableAxis(axis);
""",
))

# ============ 5. IHardwareBridge.cs：接口新增 DisableAxis ============
PATCHES.append((
    r"Services\IHardwareBridge.cs",
    "interface: DisableAxis",
    """        /// <summary>使能 / 解除使能轴（EnableLevel 等参数取自 axis）。</summary>
        void EnableAxis(AxisItem axis);
""",
    """        /// <summary>使能轴（EnableLevel 等参数取自 axis）。</summary>
        void EnableAxis(AxisItem axis);

        /// <summary>解除使能（下伺服 / 断使能）。与 EnableAxis 相对，EnableLevel 等参数同样取自 axis。</summary>
        void DisableAxis(AxisItem axis);
""",
))

# ============ 6. StubHardwareBridge.cs ============
PATCHES.append((
    r"Services\StubHardwareBridge.cs",
    "stub: DisableAxis",
    """        public void EnableAxis(AxisItem axis) =>
            Write($"轴使能 → 名称={axis.Name} 轴号={axis.AxisNo}");
""",
    """        public void EnableAxis(AxisItem axis) =>
            Write($"轴使能 → 名称={axis.Name} 轴号={axis.AxisNo}");
        public void DisableAxis(AxisItem axis) =>
            Write($"轴解除使能 → 名称={axis.Name} 轴号={axis.AxisNo}");
""",
))

# ============ 7. WenQiZhiCardBridge.cs（卡族桥）============
PATCHES.append((
    r"Services\Hardware\Cards\WenQiZhiCardBridge.cs",
    "card-family: DisableAxis",
    """            if (CardFamilyCatalog.IsBusType(slot.Config?.BusType))
            {
                // 总线伺服的 CiA402 状态机 2→3→4 不是瞬间完成的，立刻读往往还停在 2，等一下再报。
                Thread.Sleep(200);
                ReportBusAxisState(axis.Name, slot, a);
            }
        }
""",
    """            if (CardFamilyCatalog.IsBusType(slot.Config?.BusType))
            {
                // 总线伺服的 CiA402 状态机 2→3→4 不是瞬间完成的，立刻读往往还停在 2，等一下再报。
                Thread.Sleep(200);
                ReportBusAxisState(axis.Name, slot, a);
            }
        }

        public void DisableAxis(AxisItem axis)
        {
            var (slot, a) = AxisOf(axis);
            if (a == null) { WarnNoAxis(axis, "解除使能"); return; }

            if (IsSimulation(slot))
            {
                // 模拟卡 active-low SON：写 0 = 使能、写 1 = 失能（与 EnableAxis 相反）。
                int sevon = 0;
                Guard(() => sevon = a.CardAxisWriteSevonPin(1));
                Report("轴解除使能", sevon, $"[卡族·模拟卡] 轴「{axis.Name}」已解除使能（伺服使能端口置 1 = 失能）");
                return;
            }

            int res = 0;
            Guard(() => res = a.SetCardAxisDisable(slot.CardNo, Math.Max(axis.AxisNo, 0)));
            Report("轴解除使能", res, $"[卡族] 轴「{axis.Name}」已解除使能（卡{slot.CardNo} 轴{axis.AxisNo}，卡族 {slot.Family.Key}）");

            if (CardFamilyCatalog.IsBusType(slot.Config?.BusType))
            {
                // 同上：失能后状态机变化也需要一点时间才读得准。
                Thread.Sleep(200);
                ReportBusAxisState(axis.Name, slot, a);
            }
        }
""",
))

# ============ 8. LeadshineHardwareBridge.cs（雷赛）============
PATCHES.append((
    r"Services\Hardware\Leadshine\LeadshineHardwareBridge.cs",
    "leadshine: DisableAxis",
    """            if (LtdmcCard.IsBusCard)
            {
                // CiA402 状态机 2→3→4 不是瞬间完成的，立刻读往往还停在 2，等一下再报。
                Thread.Sleep(200);
                ReportBusAxisState(axis.Name, card, no, "使能后");
            }
        }
""",
    """            if (LtdmcCard.IsBusCard)
            {
                // CiA402 状态机 2→3→4 不是瞬间完成的，立刻读往往还停在 2，等一下再报。
                Thread.Sleep(200);
                ReportBusAxisState(axis.Name, card, no, "使能后");
            }
        }

        public void DisableAxis(AxisItem axis)
        {
            if (!Ready(axis.Name, "解除使能")) return;
            var (card, no) = Addr(axis);
            WarnIfAxisCardMismatch(axis, card);

            bool lowActive = IsLowActive(axis.EnableLevel);
            string path = null;
            Guard(() => path = _card.SetServoEnable(card, no, enable: false, lowActive: lowActive));
            string how = LtdmcCard.IsBusCard ? string.Empty : (lowActive ? "，低电平有效" : "，高电平有效");
            Log($"[雷赛] 轴「{axis.Name}」已解除使能（{path}{how}）");

            if (LtdmcCard.IsBusCard)
            {
                Thread.Sleep(200);
                ReportBusAxisState(axis.Name, card, no, "解除使能后");
            }
        }
""",
))


# ---------------------------------------------------------------- 工具函数
def read_source(path):
    """按 BOM 优先顺序探测编码，返回 (文本, 编码名, 行尾)。"""
    raw = open(path, "rb").read()
    for enc in ("utf-8-sig", "utf-8", "gb18030"):
        try:
            text = raw.decode(enc)
        except UnicodeDecodeError:
            continue
        nl = "\r\n" if "\r\n" in text else "\n"
        return text, enc, nl
    raise RuntimeError("无法识别文件编码：" + path)


def apply_one(text, old_lf, new_lf, tag, nl):
    old = old_lf.replace("\n", nl)
    new = new_lf.replace("\n", nl)
    hit = text.count(old)
    if hit != 1:
        raise RuntimeError(
            "锚点命中 %d 次（应为 1 次）：%s\n---- 锚点首行 ----\n%s"
            % (hit, tag, old.split(nl)[0])
        )
    return text.replace(old, new)


def main():
    print("NoCodeMotion axis enable/disable patch")
    print("root :", ROOT)
    print("mode :", "DRY-RUN (no file written)" if DRY else "APPLY")
    print("-" * 72)

    buffers = {}   # abs path -> [text, enc, nl]
    order = []

    for rel, tag, old_lf, new_lf in PATCHES:
        path = os.path.join(ROOT, rel)
        if not os.path.exists(path):
            print("FAIL  文件不存在:", rel)
            return 1
        if path not in buffers:
            buffers[path] = list(read_source(path))
            order.append(path)
        try:
            buffers[path][0] = apply_one(buffers[path][0], old_lf, new_lf, tag, buffers[path][2])
        except RuntimeError as ex:
            print("FAIL  %s  [%s]" % (rel, tag))
            print("      " + str(ex).replace("\n", "\n      "))
            print("-" * 72)
            print("锚点校验未通过，未修改任何文件。")
            return 1
        print("CHECK %-58s %s" % (rel, tag))

    print("-" * 72)
    print("全部 %d 处锚点校验通过。" % len(PATCHES))

    if DRY:
        print("DRY-RUN：不写文件。")
        return 0

    stamp = time.strftime("%Y%m%d-%H%M%S")
    backup_root = os.path.join(ROOT, "_backup", "axis-enable-" + stamp)

    for path in order:
        rel = os.path.relpath(path, ROOT)
        dst = os.path.join(backup_root, rel)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copy2(path, dst)
        text, enc, _nl = buffers[path]
        with open(path, "wb") as f:
            f.write(text.encode(enc))
        print("WROTE %-58s (encoding=%s)" % (rel, enc))
    print("备份目录：", backup_root)

    if DO_BUILD:
        print("-" * 72)
        print("运行 dotnet build 验证 ...")
        try:
            r = subprocess.run(
                ["dotnet", "build", "NoCodeMotion.csproj", "-c", "Debug", "--nologo", "-v", "q"],
                cwd=ROOT,
            )
            print("build 退出码 =", r.returncode, "(0 = 成功)")
        except Exception as ex:
            print("构建未能执行：", ex)

    print("-" * 72)
    print("完成。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
