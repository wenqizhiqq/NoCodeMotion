namespace NoCodeMotion.Models
{
    /// <summary>
    /// 点位移动条件：移动到该点位之前需满足的一条 IO / 气缸判断，
    /// 用于防止撞机（如「气缸A 必须缩回」「感应器B 必须为 1」才允许进入该点位）。
    /// 一条点位可配置多条条件，全部满足才放行。
    /// </summary>
    public class PointMoveCondition : EditorItemBase
    {
        /// <summary>条件类型：IO（输入/输出点状态）或 气缸（气缸位置）。</summary>
        public string Kind
        {
            get => _kind;
            set => SetField(ref _kind, value);
        }
        private string _kind = "IO";

        /// <summary>目标名称：来自 Catalog.IoNames（IO 点）或 Catalog.CylinderNames（气缸）。</summary>
        public string TargetName
        {
            get => _targetName;
            set => SetField(ref _targetName, value);
        }
        private string _targetName = string.Empty;

        /// <summary>
        /// 期望状态：IO 为 "0" / "1"；气缸为 "伸出" / "缩回"。
        /// </summary>
        public string ExpectedState
        {
            get => _expectedState;
            set => SetField(ref _expectedState, value);
        }
        private string _expectedState = "0";

        /// <summary>比较方式："==" 表示需等于期望状态；"!=" 表示需不等于期望状态。</summary>
        public string Comparison
        {
            get => _comparison;
            set => SetField(ref _comparison, value);
        }
        private string _comparison = "==";
    }
}
