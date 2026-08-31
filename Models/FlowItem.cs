// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.ObjectModel;
using NoCodeMotion.Models;

namespace NoCodeMotion.Models
{
    /// <summary>流程类型：表格流程（逐行步骤）、Lua 脚本流程、视觉流程（图形节点 / 视觉算子）。</summary>
    public enum FlowKind
    {
        /// <summary>表格流程：左侧列表逐行步骤执行。</summary>
        Table = 0,
        /// <summary>Lua 脚本流程：执行 Lua 源码。</summary>
        Lua = 1,
        /// <summary>视觉流程：相机 / 视觉算子 / 模板匹配 等节点（图形式编辑，先保留 Kind 入口，编辑区占位）。</summary>
        Vision = 2
    }

    /// <summary>流程角色：主流程（常规运行）/ 复位流程（设备上电或急停复位后先执行）。</summary>
    public enum FlowRole
    {
        /// <summary>主流程：常规生产运行流程。</summary>
        Main = 0,
        /// <summary>复位流程：设备上电、急停复位后先执行的归零 / 复位流程。</summary>
        Reset = 1
    }

    /// <summary>流程项目：左侧列表中的一项，自身包含若干步骤（FlowStep）。</summary>
    public class FlowItem : EditorItemBase
    {
        public ObservableCollection<FlowStep> Steps { get; set; } = new();

        /// <summary>视觉流程步骤集合（图像采集 / 模板匹配 / 缺陷检测 / 测量 / 通讯）。</summary>
        public ObservableCollection<VisualFlowStep> VisualSteps { get; set; } = new();

        private FlowKind _kind = FlowKind.Table;
        /// <summary>流程类型：表格 / 脚本。</summary>
        public FlowKind Kind
        {
            get => _kind;
            set => SetField(ref _kind, value);
        }

        private string _luaSource = string.Empty;   // 默认空字符串——新流程首次读取就是空，
                                                        // 避免 LuaEditorView 还没收到 INPC 时显示字段初始值（DefaultLuaTemplate 40 行示例）
                                                        // OldLuaSource = DefaultLuaTemplate 常量仍保留在 LuaStudio 作"重置模板"用
        /// <summary>Lua 脚本流程的源码（仅 Kind==Lua 时使用）。</summary>
        public string LuaSource
        {
            get => _luaSource;
            set => SetField(ref _luaSource, value);
        }

        private FlowRole _role = FlowRole.Main;
        /// <summary>流程角色：主流程 / 复位流程。</summary>
        public FlowRole Role
        {
            get => _role;
            set => SetField(ref _role, value);
        }

        public const string DefaultLuaTemplate =
@"-- ============ Lua 流程脚本示例 ============
-- 在编辑器中：F5 运行，F10 单步，F9 在行号左侧打断点
-- print 的内容会显示在下方「输出」面板

-- 1. 变量
local title = ""运动流程示例""
print(""开始执行："" .. title)

-- 2. 表格（可在右侧「变量」面板展开查看）
local cfg = {
    name = ""轴1"",
    speed = 120,
    target = 500,
    enabled = true
}
print(""配置："" .. cfg.name .. ""  目标位置="" .. cfg.target)

-- 3. 函数定义（F11 步入函数内部单步）
local function moveTo(pos)
    local step = 10
    print(""移动到 "" .. pos)
    return pos + step
end

-- 4. 循环（单步观察 i 与累计值变化）
local total = 0
for i = 1, 5 do
    total = moveTo(total)
    print(""第 "" .. i .. "" 步，累计="" .. total)
end

-- 5. 条件判断
if total >= cfg.target then
    print(""已到达目标位置"")
else
    print(""未到达目标，当前="" .. total)
end

-- 6. 结束：流程成功返回 true
return true";
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
