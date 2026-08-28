// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoCodeMotion.Models
{
    /// <summary>变量表的一行：包含 5 个 (名称 / 字符串值) 单元。</summary>
    public class VariableRow : INotifyPropertyChanged
    {
        private string _name1 = string.Empty;
        private string _value1 = string.Empty;
        private string _name2 = string.Empty;
        private string _value2 = string.Empty;
        private string _name3 = string.Empty;
        private string _value3 = string.Empty;
        private string _name4 = string.Empty;
        private string _value4 = string.Empty;
        private string _name5 = string.Empty;
        private string _value5 = string.Empty;

        public string Name1 { get => _name1; set => SetField(ref _name1, value); }
        public string Value1 { get => _value1; set => SetField(ref _value1, value); }
        public string Name2 { get => _name2; set => SetField(ref _name2, value); }
        public string Value2 { get => _value2; set => SetField(ref _value2, value); }
        public string Name3 { get => _name3; set => SetField(ref _name3, value); }
        public string Value3 { get => _value3; set => SetField(ref _value3, value); }
        public string Name4 { get => _name4; set => SetField(ref _name4, value); }
        public string Value4 { get => _value4; set => SetField(ref _value4, value); }
        public string Name5 { get => _name5; set => SetField(ref _name5, value); }
        public string Value5 { get => _value5; set => SetField(ref _value5, value); }

        /// <summary>该行所有非空变量名（用于同步到 Catalog，供流程页引用）。</summary>
        public IEnumerable<string> Names()
        {
            if (!string.IsNullOrWhiteSpace(Name1)) yield return Name1;
            if (!string.IsNullOrWhiteSpace(Name2)) yield return Name2;
            if (!string.IsNullOrWhiteSpace(Name3)) yield return Name3;
            if (!string.IsNullOrWhiteSpace(Name4)) yield return Name4;
            if (!string.IsNullOrWhiteSpace(Name5)) yield return Name5;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
