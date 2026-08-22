import os

ROOT = r"D:\wqz\code\NoCodeMotion"

def read_raw(path):
    with open(path, "rb") as f:
        return f.read()

def decode(b):
    if b[:3] == b"\xef\xbb\xbf":
        return b[3:].decode("utf-8"), True
    try:
        return b.decode("utf-8"), False
    except Exception:
        return b.decode("gbk"), False

def eol_of(s):
    return "\r\n" if "\r\n" in s else "\n"

def write_new(path, content, bom=False, crlf=False):
    eol = "\r\n" if crlf else "\n"
    text = eol.join(content.splitlines())
    data = text.encode("utf-8")
    if bom:
        data = b"\xef\xbb\xbf" + data
    with open(path, "wb") as f:
        f.write(data)
    print(f"  NEW/REWRITE {os.path.relpath(path, ROOT)}  ({len(data)} bytes)")

def patch_file(rel, old_lines, new_lines, skip_if=None):
    path = os.path.join(ROOT, rel)
    b = read_raw(path)
    s, bom = decode(b)
    eol = eol_of(s)
    old = eol.join(old_lines)
    new = eol.join(new_lines)
    if skip_if is not None and skip_if in s:
        print(f"  SKIP   {rel}  (already applied: {skip_if!r})")
        return
    cnt = s.count(old)
    if cnt != 1:
        print(f"  FAIL   {rel}  (old matched {cnt} times, expected 1)")
        return
    s = s.replace(old, new, 1)
    data = s.encode("utf-8")
    if bom:
        data = b"\xef\xbb\xbf" + data
    with open(path, "wb") as f:
        f.write(data)
    print(f"  PATCH  {rel}  (applied)")

# ----------------------------------------------------------------------------
# 1) Models/ProjectEntry.cs  (NEW)
# ----------------------------------------------------------------------------
pe_cs = r'''using System;

namespace NoCodeMotion.Models
{
    /// <summary>项目管理页表格的一行：工程名 + 创建时间 + 修改时间 + 备注。</summary>
    public class ProjectEntry
    {
        public string Name { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Remark { get; set; }
    }
}
'''
write_new(os.path.join(ROOT, "Models", "ProjectEntry.cs"), pe_cs, bom=False, crlf=False)

# ----------------------------------------------------------------------------
# 2) Services/ProjectManager.cs  (REWRITE)
# ----------------------------------------------------------------------------
pm_cs = r'''using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 多工程管理：在 %LocalAppData%\NoCodeMotion\Projects 下以「工程名.json」文件管理多个工程，
    /// 提供 列出 / 新建 / 打开(读取) / 保存 / 删除 / 重命名 / 改备注。
    /// 打开/新建采用「原地载入」：只替换 ProjectStore.Data 各集合的内容，不替换 Data 实例，
    /// 因此各页面 ViewModel 持有的集合引用始终有效，无需重建即可看到新工程数据。
    /// </summary>
    public static class ProjectManager
    {
        public static string RootDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "NoCodeMotion", "Projects");

        /// <summary>当前已打开/保存的工程名（未指定为 null）。</summary>
        public static string? CurrentName { get; private set; }

        /// <summary>工程数据被原地替换后触发，供主窗口清空页面缓存并重建当前页。</summary>
        public static event System.Action? DataReloaded;

        /// <summary>列出全部工程条目（含创建/修改时间、备注）。</summary>
        public static List<ProjectEntry> ListProjectEntries()
        {
            var list = new List<ProjectEntry>();
            try
            {
                if (!Directory.Exists(RootDir)) return list;
                foreach (var file in Directory.EnumerateFiles(RootDir, "*.json")
                             .OrderBy(f => Path.GetFileNameWithoutExtension(f)))
                {
                    var name = Path.GetFileNameWithoutExtension(file) ?? "";
                    DateTime? created = null, updated = null;
                    string? remark = "";
                    try
                    {
                        var data = JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(file));
                        if (data != null)
                        {
                            created = data.CreatedAt;
                            updated = data.UpdatedAt;
                            remark = data.Remark;
                        }
                    }
                    catch { }
                    if (created == null) { try { created = File.GetCreationTime(file); } catch { } }
                    if (updated == null) { try { updated = File.GetLastWriteTime(file); } catch { } }
                    updated = updated ?? created;
                    list.Add(new ProjectEntry { Name = name, CreatedAt = created, UpdatedAt = updated, Remark = remark });
                }
            }
            catch { }
            return list;
        }

        private static string FileFor(string name) => Path.Combine(RootDir, name + ".json");

        public static bool Exists(string name) => File.Exists(FileFor(name));

        /// <summary>新建工程：写入空工程文件并原地载入为当前数据。</summary>
        public static void NewProject(string name)
        {
            var fresh = new ProjectData();
            fresh.EnsurePointTables();
            fresh.CreatedAt = DateTime.Now;
            CurrentName = name;
            WriteFile(name, fresh);
            LoadInto(fresh);
        }

        /// <summary>打开(读取)工程：从 name.json 载入为当前数据（原地，不替换 Data 实例）。</summary>
        public static void OpenProject(string name)
        {
            var path = FileFor(name);
            ProjectData data;
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    data = JsonSerializer.Deserialize<ProjectData>(json) ?? new ProjectData();
                }
                catch
                {
                    data = new ProjectData();
                }
            }
            else
            {
                data = new ProjectData();
            }
            data.EnsurePointTables();
            CurrentName = name;
            LoadInto(data);
        }

        /// <summary>保存当前工程（写入 name；默认保存到当前工程名）。</summary>
        public static void SaveCurrent(string? name = null)
        {
            name = name ?? CurrentName;
            if (string.IsNullOrEmpty(name)) return;
            CurrentName = name;
            WriteFile(name, ProjectStore.Data);
        }

        /// <summary>修改指定工程的备注（改写其 JSON 文件，并更新修改时间）。</summary>
        public static void SetRemark(string name, string? remark)
        {
            var path = FileFor(name);
            if (!File.Exists(path)) return;
            try
            {
                var data = JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(path)) ?? new ProjectData();
                data.Remark = remark ?? "";
                WriteFile(name, data);
            }
            catch { }
        }

        public static void DeleteProject(string name)
        {
            try { File.Delete(FileFor(name)); } catch { }
            if (CurrentName == name) CurrentName = null;
        }

        public static void RenameProject(string oldName, string newName)
        {
            try
            {
                if (oldName != newName && File.Exists(FileFor(oldName)))
                    File.Move(FileFor(oldName), FileFor(newName), true);
            }
            catch { }
            if (CurrentName == oldName) CurrentName = newName;
        }

        private static void WriteFile(string name, ProjectData data)
        {
            try
            {
                Directory.CreateDirectory(RootDir);
                data.UpdatedAt = DateTime.Now;
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FileFor(name), json);
            }
            catch { }
        }

        /// <summary>把 src 内容原地复制到 ProjectStore.Data（保留集合实例），同步名称库后通知界面重建。</summary>
        private static void LoadInto(ProjectData src)
        {
            ProjectStore.SuppressSave(true);
            try
            {
                ProjectStore.Data.CopyFrom(src);
                ProjectStore.Data.EnsurePointTables();
                Catalog.SyncAllFromData(ProjectStore.Data);
            }
            finally
            {
                ProjectStore.SuppressSave(false);
            }
            DataReloaded?.Invoke();
        }
    }
}
'''
write_new(os.path.join(ROOT, "Services", "ProjectManager.cs"), pm_cs, bom=False, crlf=False)

# ----------------------------------------------------------------------------
# 3) ViewModels/ProjectManagerViewModel.cs  (REWRITE)
# ----------------------------------------------------------------------------
pmvm_cs = r'''using System.Collections.ObjectModel;
using System.Windows.Input;
using NoCodeMotion.Models;
using NoCodeMotion.Services;
using NoCodeMotion.Views;

namespace NoCodeMotion.ViewModels
{
    /// <summary>项目管理页面：以表格列出全部工程（名称/创建时间/修改时间/备注），并提供 新建 / 打开(读取) / 保存 / 删除 / 重命名 / 刷新。</summary>
    public class ProjectManagerViewModel : ViewModelBase, IEnsureDefaultSelection
    {
        public ObservableCollection<ProjectEntry> Projects { get; } = new();

        private ProjectEntry? _selectedEntry;
        public ProjectEntry? SelectedEntry
        {
            get => _selectedEntry;
            set => SetField(ref _selectedEntry, value);
        }

        public string? CurrentProject => ProjectManager.CurrentName;

        public ICommand NewCommand => new RelayCommand(_ => New());
        public ICommand OpenCommand => new RelayCommand(_ => Open(), _ => SelectedEntry != null);
        public ICommand SaveCommand => new RelayCommand(_ => Save());
        public ICommand DeleteCommand => new RelayCommand(_ => Delete(), _ => SelectedEntry != null);
        public ICommand RenameCommand => new RelayCommand(_ => Rename(), _ => SelectedEntry != null);
        public ICommand RefreshCommand => new RelayCommand(_ => Refresh());

        public ProjectManagerViewModel()
        {
            Refresh();
        }

        private void Refresh()
        {
            Projects.Clear();
            foreach (var e in ProjectManager.ListProjectEntries())
                Projects.Add(e);
            OnPropertyChanged(nameof(CurrentProject));
        }

        /// <summary>备注单元格编辑结束后调用：把备注写回对应工程文件。</summary>
        public void PersistRemark(ProjectEntry entry) => ProjectManager.SetRemark(entry.Name, entry.Remark);

        private void New()
        {
            var dlg = new RenameDialog("新建工程", "工程" + (Projects.Count + 1));
            if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.ResultName)) return;
            var name = dlg.ResultName!;
            if (ProjectManager.Exists(name))
                ProjectManager.OpenProject(name);
            else
                ProjectManager.NewProject(name);
            // 新建/打开会触发界面重建，列表由重建后的页面重新刷新，此处无需额外 Refresh
        }

        private void Open()
        {
            if (SelectedEntry == null) return;
            ProjectManager.OpenProject(SelectedEntry.Name);
        }

        private void Save()
        {
            string? name = SelectedEntry?.Name ?? ProjectManager.CurrentName;
            if (string.IsNullOrEmpty(name))
            {
                var dlg = new RenameDialog("保存工程", "工程1");
                if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.ResultName)) return;
                name = dlg.ResultName!;
            }
            ProjectManager.SaveCurrent(name);
            Refresh();
            SelectedEntry = Projects.FirstOrDefault(p => p.Name == name);
        }

        private void Delete()
        {
            if (SelectedEntry == null) return;
            var dlg = new ConfirmDialog("删除工程", $"确定删除工程「{SelectedEntry.Name}」？此操作不可撤销。");
            if (dlg.ShowDialog() != true) return;
            ProjectManager.DeleteProject(SelectedEntry.Name);
            Refresh();
            SelectedEntry = null;
        }

        private void Rename()
        {
            if (SelectedEntry == null) return;
            var dlg = new RenameDialog("重命名工程", SelectedEntry.Name);
            if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.ResultName)) return;
            var newName = dlg.ResultName!;
            if (newName != SelectedEntry.Name && ProjectManager.Exists(newName)) return;
            ProjectManager.RenameProject(SelectedEntry.Name, newName);
            Refresh();
            SelectedEntry = Projects.FirstOrDefault(p => p.Name == newName);
        }

        public void EnsureDefaultSelection()
        {
            Refresh();
            if (SelectedEntry == null && Projects.Count > 0)
                SelectedEntry = Projects[0];
        }
    }
}
'''
write_new(os.path.join(ROOT, "ViewModels", "ProjectManagerViewModel.cs"), pmvm_cs, bom=False, crlf=False)

# ----------------------------------------------------------------------------
# 4) Models/ProjectData.cs  -- add metadata props + copy them in CopyFrom
# ----------------------------------------------------------------------------
patch_file(
    "Models/ProjectData.cs",
    [
        '        /// <summary>变量表（流程/逻辑中可引用的计算与状态变量），每行含 5 个 (名称/字符串值)。</summary>',
        '        public ObservableCollection<VariableRow> Variables { get; set; } = new();',
    ],
    [
        '        /// <summary>变量表（流程/逻辑中可引用的计算与状态变量），每行含 5 个 (名称/字符串值)。</summary>',
        '        public ObservableCollection<VariableRow> Variables { get; set; } = new();',
        '',
        '        /// <summary>工程创建时间（首次保存时写入）。</summary>',
        '        public DateTime? CreatedAt { get; set; }',
        '',
        '        /// <summary>工程最后修改时间（每次保存时更新）。</summary>',
        '        public DateTime? UpdatedAt { get; set; }',
        '',
        '        /// <summary>工程备注（自由文本，可在项目管理页编辑）。</summary>',
        '        public string? Remark { get; set; }',
    ],
    skip_if="public DateTime? CreatedAt",
)
patch_file(
    "Models/ProjectData.cs",
    [
        '            Variables.Clear(); foreach (var x in src.Variables) Variables.Add(x);',
    ],
    [
        '            Variables.Clear(); foreach (var x in src.Variables) Variables.Add(x);',
        '            CreatedAt = src.CreatedAt;',
        '            UpdatedAt = src.UpdatedAt;',
        '            Remark = src.Remark;',
    ],
    skip_if="CreatedAt = src.CreatedAt;",
)

# ----------------------------------------------------------------------------
# 5) Views/ProjectManagerPage.xaml  (REWRITE -> DataGrid table)
# ----------------------------------------------------------------------------
pmp_xaml = r'''<UserControl x:Class="NoCodeMotion.Views.ProjectManagerPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:local="clr-namespace:NoCodeMotion.Views"
             mc:Ignorable="d" d:DesignHeight="450" d:DesignWidth="800">

    <Grid Background="{StaticResource CanvasBrush}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 顶部工具栏：新建 / 打开 / 保存 / 删除 / 重命名 / 刷新 -->
        <Border Grid.Row="0" Style="{StaticResource ToolbarStyle}">
            <StackPanel Orientation="Horizontal">
                <Button Command="{Binding NewCommand}" Style="{StaticResource ToolButtonStyle}">
                    <StackPanel Orientation="Horizontal">
                        <Path Data="{StaticResource AddIcon}" Width="15" Height="15" Stretch="Uniform" Margin="0,0,5,0" VerticalAlignment="Center" Fill="{Binding RelativeSource={RelativeSource AncestorType=Button}, Path=Foreground}"/>
                        <TextBlock Text="新建" VerticalAlignment="Center"/>
                    </StackPanel>
                </Button>
                <Button Command="{Binding OpenCommand}" Style="{StaticResource ToolButtonStyle}">
                    <TextBlock Text="打开" VerticalAlignment="Center" Margin="6,0"/>
                </Button>
                <Button Command="{Binding SaveCommand}" Style="{StaticResource ToolButtonStyle}">
                    <TextBlock Text="保存" VerticalAlignment="Center" Margin="6,0"/>
                </Button>
                <Button Command="{Binding DeleteCommand}" Style="{StaticResource ToolButtonStyle}">
                    <StackPanel Orientation="Horizontal">
                        <Path Data="{StaticResource DeleteIcon}" Width="15" Height="15" Stretch="Uniform" Margin="0,0,5,0" VerticalAlignment="Center" Fill="{Binding RelativeSource={RelativeSource AncestorType=Button}, Path=Foreground}"/>
                        <TextBlock Text="删除" VerticalAlignment="Center"/>
                    </StackPanel>
                </Button>
                <Button Command="{Binding RenameCommand}" Style="{StaticResource ToolButtonStyle}">
                    <TextBlock Text="重命名" VerticalAlignment="Center" Margin="6,0"/>
                </Button>
                <Button Command="{Binding RefreshCommand}" Style="{StaticResource ToolButtonStyle}">
                    <TextBlock Text="刷新" VerticalAlignment="Center" Margin="6,0"/>
                </Button>
                <TextBlock Text="{Binding Projects.Count, StringFormat=共 {0} 个工程}" VerticalAlignment="Center" Margin="16,0,0,0" Foreground="{StaticResource TextSecondaryBrush}"/>
            </StackPanel>
        </Border>

        <!-- 当前工程提示 -->
        <Border Grid.Row="1" Style="{StaticResource DetailPanelStyle}" Margin="12,12,12,0">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="当前工程：" Foreground="{StaticResource TextSecondaryBrush}"/>
                <TextBlock Text="{Binding CurrentProject, TargetNullValue=未指定}" FontWeight="SemiBold" Foreground="{StaticResource TextPrimaryBrush}"/>
                <TextBlock Text="　·　双击列表中的工程可快速打开；备注列可直接编辑" Foreground="{StaticResource TextMutedBrush}" Margin="16,0,0,0"/>
            </StackPanel>
        </Border>

        <!-- 工程表格：名称 / 创建时间 / 修改时间 / 备注 -->
        <DataGrid Grid.Row="2" Margin="12"
                  ItemsSource="{Binding Projects}"
                  SelectedItem="{Binding SelectedEntry, Mode=TwoWay}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False" CanUserDeleteRows="False"
                  GridLinesVisibility="Horizontal"
                  HeadersVisibility="Column"
                  BorderBrush="{StaticResource LineBrush}"
                  Background="{StaticResource SurfaceBrush}"
                  RowBackground="{StaticResource SurfaceBrush}"
                  AlternatingRowBackground="{StaticResource CanvasBrush}"
                  Foreground="{StaticResource TextPrimaryBrush}"
                  MouseDoubleClick="ProjectGrid_DoubleClick"
                  CellEditEnding="ProjectGrid_CellEditEnding">
            <DataGrid.ColumnHeaderStyle>
                <Style TargetType="DataGridColumnHeader">
                    <Setter Property="Background" Value="{StaticResource HeaderBrush}"/>
                    <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
                    <Setter Property="Padding" Value="8,6"/>
                    <Setter Property="BorderBrush" Value="{StaticResource LineBrush}"/>
                    <Setter Property="BorderThickness" Value="0,0,0,1"/>
                    <Setter Property="HorizontalContentAlignment" Value="Left"/>
                </Style>
            </DataGrid.ColumnHeaderStyle>
            <DataGrid.Columns>
                <DataGridTextColumn Header="工程名称" Binding="{Binding Name}" IsReadOnly="True" Width="*"/>
                <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt, StringFormat='yyyy-MM-dd HH:mm'}" IsReadOnly="True" Width="160"/>
                <DataGridTextColumn Header="修改时间" Binding="{Binding UpdatedAt, StringFormat='yyyy-MM-dd HH:mm'}" IsReadOnly="True" Width="160"/>
                <DataGridTextColumn Header="备注" Binding="{Binding Remark, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
'''
write_new(os.path.join(ROOT, "Views", "ProjectManagerPage.xaml"), pmp_xaml, bom=True, crlf=True)

# ----------------------------------------------------------------------------
# 6) Views/ProjectManagerPage.xaml.cs  -- add using + replace handlers
# ----------------------------------------------------------------------------
patch_file(
    "Views/ProjectManagerPage.xaml.cs",
    [
        "using NoCodeMotion.ViewModels;",
    ],
    [
        "using NoCodeMotion.ViewModels;",
        "using NoCodeMotion.Models;",
    ],
    skip_if="using NoCodeMotion.Models;",
)
patch_file(
    "Views/ProjectManagerPage.xaml.cs",
    [
        "        private void ProjectList_DoubleClick(object sender, MouseButtonEventArgs e)",
        "        {",
        "            if (DataContext is ProjectManagerViewModel vm && vm.OpenCommand.CanExecute(null))",
        "                vm.OpenCommand.Execute(null);",
        "        }",
    ],
    [
        "        private void ProjectGrid_DoubleClick(object sender, MouseButtonEventArgs e)",
        "        {",
        "            if (DataContext is ProjectManagerViewModel vm && vm.OpenCommand.CanExecute(null))",
        "                vm.OpenCommand.Execute(null);",
        "        }",
        "",
        "        private void ProjectGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)",
        "        {",
        "            if (e.Column.Header?.ToString() == \"备注\" &&",
        "                e.Row.Item is ProjectEntry entry &&",
        "                DataContext is ProjectManagerViewModel vm)",
        "            {",
        "                vm.PersistRemark(entry);",
        "            }",
        "        }",
    ],
    skip_if="ProjectGrid_CellEditEnding",
)

# ----------------------------------------------------------------------------
# 7) Validate
# ----------------------------------------------------------------------------
import xml.etree.ElementTree as ET
def s2(b):
    return b[3:].decode("utf-8") if b[:3]==b"\xef\xbb\xbf" else (b.decode("utf-8") if b"\x00" not in b[:50] else b.decode("gbk"))

print("\n--- VALIDATE ---")
try:
    ET.parse(os.path.join(ROOT,"Views/ProjectManagerPage.xaml")); print("XML OK   Views/ProjectManagerPage.xaml")
except Exception as e:
    print("XML FAIL", e)

def braces(p):
    s=s2(read_raw(os.path.join(ROOT,p)))
    bal={'(':0,'{':0,'[':0}
    for ch in s:
        if ch=='(' : bal['(']+=1
        elif ch==')': bal['(']-=1
        elif ch=='{': bal['{']+=1
        elif ch=='}': bal['{']-=1
        elif ch=='[': bal['[']+=1
        elif ch==']': bal['[']-=1
    return bal
for c in ["Services/ProjectManager.cs","ViewModels/ProjectManagerViewModel.cs",
          "Views/ProjectManagerPage.xaml.cs","Models/ProjectData.cs","Models/ProjectEntry.cs"]:
    bb=braces(c); ok = bb['(']==0 and bb['{']==0 and bb['[']==0
    print(("CS OK  " if ok else "CS FAIL"), c, bb)

checks = {
 "Models/ProjectData.cs": ["public DateTime? CreatedAt","public DateTime? UpdatedAt","public string? Remark","CreatedAt = src.CreatedAt;"],
 "Models/ProjectEntry.cs": ["class ProjectEntry","public DateTime? CreatedAt","public string? Remark"],
 "Services/ProjectManager.cs": ["ListProjectEntries","SetRemark","data.UpdatedAt = DateTime.Now;","fresh.CreatedAt = DateTime.Now;"],
 "ViewModels/ProjectManagerViewModel.cs": ["ObservableCollection<ProjectEntry>","SelectedEntry","PersistRemark","Projects.FirstOrDefault"],
 "Views/ProjectManagerPage.xaml": ["<DataGrid","创建时间","修改时间","备注","ProjectGrid_DoubleClick","ProjectGrid_CellEditEnding"],
 "Views/ProjectManagerPage.xaml.cs": ["using NoCodeMotion.Models;","ProjectGrid_DoubleClick","ProjectGrid_CellEditEnding","PersistRemark"],
}
for f,keys in checks.items():
    s=s2(read_raw(os.path.join(ROOT,f))); miss=[k for k in keys if k not in s]
    print(("OK  " if not miss else "MISS"), f, ("" if not miss else "-> "+str(miss)))

# ----------------------------------------------------------------------------
# 8) Memory note (append)
# ----------------------------------------------------------------------------
mp = os.path.join(ROOT, ".workbuddy", "memory", "2026-08-20.md")
mb = read_raw(mp); mbom = mb[:3]==b"\xef\xbb\xbf"
ms = mb[3:].decode("utf-8") if mbom else (mb.decode("utf-8") if b"\x00" not in mb[:50] else mb.decode("gbk"))
meol = "\r\n" if "\r\n" in ms else "\n"
note = (
    meol + meol +
    "### 项目管理页增加表格列（创建时间/修改时间/备注）" + meol +
    "- 需求补充：项目管理页表格需列 创建时间、修改时间、备注（原为纯名称 ListBox）。" + meol +
    "- ProjectData 新增 CreatedAt/UpdatedAt(DateTime?) + Remark(string?)；CopyFrom 同步这 3 个标量（打开工程后当前工程元数据正确）。" + meol +
    "- 新增 Models/ProjectEntry.cs（Name/CreatedAt/UpdatedAt/Remark）；ProjectManager.ListProjectEntries() 反序列化各工程文件取元数据（旧文件无字段则回退到文件 创建/最后写入 时间）。" + meol +
    "- ProjectManager.WriteFile 统一写盘前置 UpdatedAt=Now；SetRemark(name,remark) 改写备注并更新修改时间；NewProject 置 CreatedAt=Now。" + meol +
    "- 页面 ProjectManagerPage.xaml 改用 DataGrid（工程名称/创建时间/修改时间/备注），备注列 UpdateSourceTrigger=PropertyChanged 可内联编辑，提交时 CellEditEnding -> vm.PersistRemark 写回文件。VM 由 ObservableCollection<string> 改为 ObservableCollection<ProjectEntry>，选中项 SelectedEntry。" + meol
)
with open(mp, "wb") as f:
    f.write((ms + note).encode("utf-8"))
print("\nMEMORY appended.")
print("\nDONE.")
