// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// 气缸时序动作表的一行：气缸名 + 动作(伸出/缩回) + 本步后延时(ms)。
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>气缸时序动作表步骤（DataGrid 行内可编辑）。</summary>
    public class CylinderSequenceStep : INotifyPropertyChanged
    {
        private int _stepIndex;
        private string _cylinder = "";
        private string _action = "伸出";
        private int _delayMs = 300;

        /// <summary>步序（由 CylinderViewModel 在集合变更后重新编号，1 起）。</summary>
        public int StepIndex { get => _stepIndex; set => Set(ref _stepIndex, value); }
        /// <summary>气缸名（从 Catalog.CylinderNames 下拉选择）。</summary>
        public string Cylinder { get => _cylinder; set => Set(ref _cylinder, value); }
        /// <summary>动作：伸出 / 缩回。</summary>
        public string Action { get => _action; set => Set(ref _action, value); }
        /// <summary>本步执行后的保持延时(ms)，用于编排多气缸时序。</summary>
        public int DelayMs { get => _delayMs; set => Set(ref _delayMs, value); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨ۤ▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣ۤ
