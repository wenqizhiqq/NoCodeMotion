// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启​志⁣◆​编⁣写⁣◇⁣微​信⁣﹕‎1‍8⁣7⁠◆‍1⁣9⁠3⁠6‏◇‍1‍3⁣9⁠9‌　‎※⁠保​留‎所​有⁠权​利‏请‎勿‍删‌除⁣◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using NoCodeMotion.Services;

namespace NoCodeMotion.Models
{
    /// <summary>
    /// 点位移动条件：移动到该点位之前需满足的一条判断，
    /// 用于防止撞机（如「气缸A 必须缩回」「感应器B 必须为 1」「轴1 位置 ≥ 100」才允许进入该点位）。
    /// 一个点位可配置多条条件，全部满足才放行。
    ///
    /// 条件类型由 <see cref="ConditionKinds"/> 统一定义，本类只负责存值 +
    /// 把该类型的「名称候选 / 期望值候选 / 运算符候选」暴露给界面绑定。
    /// </summary>
    public class PointMoveCondition : EditorItemBase
    {
        /// <summary>条件类型：见 <see cref="ConditionKinds.All"/>（IO / 气缸 / 变量 / 轴位置 / 轴使能 / 轴速度 / 轴正限位 / 轴负限位 / 相机）。</summary>
        public string Kind
        {
            get => _kind;
            set
            {
                if (!SetField(ref _kind, value)) return;
                // 类型变了：名称候选 / 期望值候选 / 运算符候选 / 是否数值型 都要重新算
                OnPropertyChanged(nameof(TargetOptions));
                OnPropertyChanged(nameof(ValueOptions));
                OnPropertyChanged(nameof(OperatorOptions));
                OnPropertyChanged(nameof(IsNumericKind));
                OnPropertyChanged(nameof(ValueHint));
                NormalizeExpectedState();
            }
        }
        private string _kind = ConditionKinds.Io;

        /// <summary>
        /// 切换类型后把期望值拉回该类型的合法取值，避免下拉框显示空白、却仍留着上一个类型的旧值：
        /// 枚举型 → 取第一个候选项（如 IO 的 "0"）；数值型 → 当前值不是数字则归 "0"。
        /// </summary>
        private void NormalizeExpectedState()
        {
            var options = ValueOptions;
            if (options.Count > 0)
            {
                foreach (var o in options)
                    if (o == _expectedState) return;
                ExpectedState = options[0];
            }
            else if (!double.TryParse(_expectedState, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                ExpectedState = "0";
            }
        }

        /// <summary>目标名称：按类型分别取自 Catalog 的 IO / 气缸 / 变量 / 轴 / 相机名称库。</summary>
        public string TargetName
        {
            get => _targetName;
            set => SetField(ref _targetName, value);
        }
        private string _targetName = string.Empty;

        /// <summary>
        /// 期望值：枚举型填状态文本（IO 为 "0"/"1"、气缸为 "伸出"/"缩回"、
        /// 轴使能为 "已使能"/"未使能"、相机为 "已连接"/"未连接"）；
        /// 数值型（变量 / 轴位置 / 轴速度 / 轴正限位 / 轴负限位）直接填数字。
        /// </summary>
        public string ExpectedState
        {
            get => _expectedState;
            set => SetField(ref _expectedState, value);
        }
        private string _expectedState = "0";

        /// <summary>比较方式：枚举型只用 "==" / "!="；数值型还可用 "&gt;" "&gt;=" "&lt;" "&lt;="。</summary>
        public string Comparison
        {
            get => _comparison;
            set => SetField(ref _comparison, value);
        }
        private string _comparison = "==";

        // ===================== 供界面绑定的类型元数据（只读，随 Kind 变化通知） =====================

        /// <summary>「名称」列下拉候选（引用 Catalog 的实时集合，配置一变下拉立刻刷新）。</summary>
        public ObservableCollection<string> TargetOptions => ConditionKinds.TargetsFor(_kind);

        /// <summary>「期望值」列下拉候选；空集合表示该类型需要自由输入数字。</summary>
        public IReadOnlyList<string> ValueOptions => ConditionKinds.ValuesFor(_kind);

        /// <summary>「比较」列下拉候选。</summary>
        public IReadOnlyList<string> OperatorOptions => ConditionKinds.OperatorsFor(_kind);

        /// <summary>是否数值型：为真时期望值列显示文本框而不是下拉框。</summary>
        public bool IsNumericKind => ConditionKinds.IsNumeric(_kind);

        /// <summary>期望值输入框的水印提示（如「位置」）。</summary>
        public string ValueHint => ConditionKinds.ValueHintFor(_kind);
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
