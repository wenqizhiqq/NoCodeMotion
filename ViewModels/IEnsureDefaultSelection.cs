namespace NoCodeMotion.ViewModels
{
    /// <summary>
    /// 页面 ViewModel 实现该接口后，MainWindow 切换到该页面时，
    /// 若页面内的主列表/表格尚未选中任何项，会自动选中第一项，避免空白和工具栏无目标。
    /// </summary>
    public interface IEnsureDefaultSelection
    {
        void EnsureDefaultSelection();
    }
}
