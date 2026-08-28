// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>变量页 ViewModel：单个表格面板，每行含 5 个 (名称/字符串值)。</summary>
    public class VariableViewModel : TablePanelViewModel<VariableRow>, IEnsureDefaultSelection
    {
        public VariableViewModel() : base("变量", ProjectStore.Data.Variables) { }

        protected override VariableRow MakeNew(int index) => new VariableRow();

        protected override VariableRow Clone(VariableRow src)
        {
            var json = JsonSerializer.Serialize(src);
            return JsonSerializer.Deserialize<VariableRow>(json)!;
        }

        protected override void OnItemChanged(VariableRow item, string? propertyName)
        {
            // 任一变量名变化时，同步到 Catalog，供流程页「名称」列（功能=变量）引用
            Catalog.SetVariable(ProjectStore.Data.Variables.SelectMany(r => r.Names()));
        }

        /// <summary>Excel 回读替换后，名称变化发生在订阅之前 → 主动全量同步一次目录。</summary>
        protected override void OnAfterExcelReplace(IList<VariableRow> imported)
            => Catalog.SetVariable(ProjectStore.Data.Variables.SelectMany(r => r.Names()));
    
        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
        }
}
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
