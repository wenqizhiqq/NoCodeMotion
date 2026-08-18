using NoCodeMotion.Models;
using NoCodeMotion.Services;

namespace NoCodeMotion.ViewModels
{
    /// <summary>点位表页 ViewModel：左侧维护“点位”列表，右侧编辑每个点位的各轴目标坐标。</summary>
    public class PointViewModel : ListEditorViewModel<PointItem>, IEnsureDefaultSelection
    {
        public PointViewModel()
        {
            CatalogCategory = "Point";
            Items = ProjectStore.Data.Points;
            Counter = Items.Count;
            AttachAutoSave();
        }

        protected override PointItem CreateNewItem()
        {
            var point = new PointItem { Name = $"点位{Counter + 1}" };
            // 为每个已配置轴初始化一行坐标（轴删减后旧点位不会自动增删，可重建点位）
            foreach (var axis in Catalog.AxisNames)
                point.Positions.Add(new PointAxis { AxisName = axis });
            return point;
        }

        public void EnsureDefaultSelection()
        {
            if (SelectedItem == null && Items.Count > 0)
                SelectedItem = Items[0];
        }
    }
}
