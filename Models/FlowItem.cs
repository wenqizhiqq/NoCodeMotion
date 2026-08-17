using System.Collections.ObjectModel;
using NoCodeMotion.Models;

namespace NoCodeMotion.Models
{
    /// <summary>流程项目：左侧列表中的一项，自身包含若干步骤（FlowStep）。</summary>
    public class FlowItem : EditorItemBase
    {
        public ObservableCollection<FlowStep> Steps { get; } = new();
    }
}
