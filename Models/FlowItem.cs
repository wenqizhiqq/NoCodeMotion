using System.Collections.ObjectModel;
using NoCodeMotion.Models;

namespace NoCodeMotion.Models
{
    /// <summary>流程类型：表格流程（逐行步骤）或 Lua 脚本流程。</summary>
    public enum FlowKind
    {
        /// <summary>表格流程：左侧列表逐行步骤执行。</summary>
        Table = 0,
        /// <summary>Lua 脚本流程：执行 Lua 源码。</summary>
        Lua = 1
    }

    /// <summary>流程项目：左侧列表中的一项，自身包含若干步骤（FlowStep）。</summary>
    public class FlowItem : EditorItemBase
    {
        public ObservableCollection<FlowStep> Steps { get; set; } = new();

        private FlowKind _kind = FlowKind.Table;
        /// <summary>流程类型：表格 / 脚本。</summary>
        public FlowKind Kind
        {
            get => _kind;
            set => SetField(ref _kind, value);
        }

        private string _luaSource = DefaultLuaTemplate;
        /// <summary>Lua 脚本流程的源码（仅 Kind==Lua 时使用）。</summary>
        public string LuaSource
        {
            get => _luaSource;
            set => SetField(ref _luaSource, value);
        }

        public const string DefaultLuaTemplate =
@"-- Lua 流程脚本
-- 在此编写流程逻辑，返回 true 表示成功
return true";
    }
}
