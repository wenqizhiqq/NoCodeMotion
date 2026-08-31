"""
重写 EditorPage.xaml.cs + EditorPage.xaml + CylinderPage.xaml：
- EditorPage 新增 LeftListItemTemplate DP
- EditorPage ListBox 用 TemplateBinding 优先用新模板
- CylinderPage 定义含 伸出/缩回 内联按钮的 ListItem 模板
- 删掉 CylinderPage 右侧的"手动动作"卡片
"""
import os, re

BASE = r"D:\wqz\code\NoCodeMotion"

# ============ 1) EditorPage.xaml.cs：已通过上一个 Python 重写完成 ============
# (已加入 LeftListItemTemplate DP + 注释更新)

# ============ 2) EditorPage.xaml：ListBox 改用 TemplateBinding ============
p = os.path.join(BASE, r"Views\EditorPage.xaml")
with open(p, "r", encoding="utf-8") as f:
    raw = f.read()
# 改 ListBox 的 ItemTemplate
old = 'ItemTemplate="{StaticResource EditorListItemTemplate}"'
new = 'ItemTemplate="{Binding LeftListItemTemplate, RelativeSource={RelativeSource AncestorType=local:EditorPage}, FallbackValue={StaticResource EditorListItemTemplate}}"'
assert old in raw, "EditorPage.xaml: ListBox ItemTemplate line not found"
raw = raw.replace(old, new, 1)
with open(p, "w", encoding="utf-8", newline="\n") as f:
    f.write(raw)
print(f"  patched {p}")

# ============ 3) CylinderPage.xaml：重写整个文件 ============
# 用 Python 保留前 3 行作者签名 + 重写主体
p2 = os.path.join(BASE, r"Views\CylinderPage.xaml")
with open(p2, "r", encoding="utf-8") as f:
    raw2 = f.read()
lines2 = raw2.splitlines(keepends=True)
header2 = "".join(lines2[:3])
# 留 footer (最后 3 行装饰)
footer2 = "\n" + "".join(lines2[-3:])

new_xaml = '''<UserControl x:Class="NoCodeMotion.Views.CylinderPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:local="clr-namespace:NoCodeMotion.Views"
             xmlns:ctl="clr-namespace:NoCodeMotion.Views.Controls"
             xmlns:svc="clr-namespace:NoCodeMotion.Services"
             xmlns:hc="https://handyorg.github.io/handycontrol"
             mc:Ignorable="d" d:DesignHeight="450" d:DesignWidth="800">

    <!--
        气缸页：左侧列表 + 右侧详情。
        左侧每行带"伸出/缩回"内联按钮 —— 列表行模板用 EditorPage.LeftListItemTemplate
        覆盖默认模板（气缸命名空间下）。
        右侧详情保留基本配置（名称/IO/动作参数/安全/高级）—— 手动动作按钮已移至左侧列表。
    -->
    <UserControl.Resources>
        <!-- 气缸页专用的列表项模板：名称 + 伸出/缩回 内联按钮 -->
        <DataTemplate x:Key="CylinderListItemTemplate">
            <Grid HorizontalAlignment="Stretch" VerticalAlignment="Center">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0" Text="{Binding Name}" VerticalAlignment="Center"
                           TextTrimming="CharacterEllipsis" Foreground="{StaticResource TextPrimaryBrush}"
                           FontSize="13" Margin="12,0,6,0"/>
                <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center" Margin="0,0,8,0">
                    <Button Content="伸出" FontSize="11" Height="22" MinWidth="36"
                            Padding="6,0" Margin="2,0"
                            Background="{StaticResource AccentBrush}" Foreground="White"
                            BorderThickness="0" Cursor="Hand"
                            Command="{Binding DataContext.ExtendCommand,
                                              RelativeSource={RelativeSource AncestorType=ListBox}}"
                            ToolTip="伸出当前气缸"/>
                    <Button Content="缩回" FontSize="11" Height="22" MinWidth="36"
                            Padding="6,0" Margin="2,0"
                            Background="{StaticResource TextSecondaryBrush}" Foreground="White"
                            BorderThickness="0" Cursor="Hand"
                            Command="{Binding DataContext.RetractCommand,
                                              RelativeSource={RelativeSource AncestorType=ListBox}}"
                            ToolTip="缩回当前气缸"/>
                </StackPanel>
            </Grid>
        </DataTemplate>

        <!--
            模板选择器：默认用 EditorPage 自带的 EditorListItemTemplate；
            如果宿主页注入了 LeftListItemTemplate（气缸页会注入 CylinderListItemTemplate）则用之。
            实际更简单的做法是：在 EditorPage 的 ListBox.ItemTemplate 上直接用
            ItemTemplate="{Binding LeftListItemTemplate, ..., FallbackValue={StaticResource EditorListItemTemplate}}"
            （已在 EditorPage.xaml 中实现），这里就不再重复覆盖。
        -->
    </UserControl.Resources>

    <local:EditorPage>
        <!-- 注入：列表项模板换成含 伸出/缩回 按钮的版本 -->
        <local:EditorPage.LeftListItemTemplate>
            <DataTemplate>
                <!-- 透传到 UserControl.Resources 里定义的模板，简化重复 -->
                <ContentControl ContentTemplate="{StaticResource CylinderListItemTemplate}" Content="{Binding}"/>
            </DataTemplate>
        </local:EditorPage.LeftListItemTemplate>

        <local:EditorPage.Detail>
            <Grid Background="{StaticResource CanvasBrush}">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <!-- 顶部标题（跨两列） -->
                <StackPanel Grid.Row="0" Grid.ColumnSpan="2" Style="{StaticResource PageHeaderPanel}">
                    <Path Style="{StaticResource PageHeaderIcon}" Data="{StaticResource CylinderIcon}"/>
                    <TextBlock Style="{StaticResource PageHeaderTitle}" Text="气缸配置"/>
                </StackPanel>

                <!-- ============ 左列：基本信息 + 动作参数 ============ -->
                <StackPanel Grid.Row="1" Grid.Column="0" Margin="12,0,6,12" VerticalAlignment="Top">

                    <!-- 基本信息 -->
                    <Border Style="{StaticResource AppleGroupCard}">
                        <StackPanel>
                            <TextBlock Style="{StaticResource AppleSectionHeader}" Text="基本信息"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="名称"/>
                                <TextBox DockPanel.Dock="Right" Style="{StaticResource AppleTextField}" Text="{Binding SelectedItem.Name, UpdateSourceTrigger=PropertyChanged}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="设备编号"/>
                                <TextBox DockPanel.Dock="Right" Style="{StaticResource AppleTextField}" Text="{Binding SelectedItem.DeviceId, UpdateSourceTrigger=PropertyChanged}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="气缸类型"/>
                                <ListBox DockPanel.Dock="Right" ItemsSource="{Binding TypeOptions}" SelectedItem="{Binding SelectedItem.Type, UpdateSourceTrigger=PropertyChanged}" Style="{StaticResource ApplePillList}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="默认动作"/>
                                <ListBox DockPanel.Dock="Right" ItemsSource="{Binding ActionOptions}" SelectedItem="{Binding SelectedItem.Action, UpdateSourceTrigger=PropertyChanged}" Style="{StaticResource ApplePillList}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="初始状态"/>
                                <ListBox DockPanel.Dock="Right" ItemsSource="{Binding InitialStateOptions}" SelectedItem="{Binding SelectedItem.InitialState, UpdateSourceTrigger=PropertyChanged}" Style="{StaticResource ApplePillList}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="备注"/>
                                <TextBox DockPanel.Dock="Right" Style="{StaticResource AppleTextField}" Text="{Binding SelectedItem.Remark, UpdateSourceTrigger=PropertyChanged}"/>
                            </DockPanel>
                        </StackPanel>
                    </Border>

                    <!-- 动作参数 -->
                    <Border Style="{StaticResource AppleGroupCard}">
                        <StackPanel>
                            <TextBlock Style="{StaticResource AppleSectionHeader}" Text="动作参数"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="动作延时"/>
                                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">
                                    <ctl:NumSliderBox Value="{Binding SelectedItem.DelayMs}" Min="0" Max="10000"/>
                                    <TextBlock Style="{StaticResource AppleUnit}" Text="ms"/>
                                </StackPanel>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="伸出延时"/>
                                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">
                                    <ctl:NumSliderBox Value="{Binding SelectedItem.ExtendMs}" Min="0" Max="10000"/>
                                    <TextBlock Style="{StaticResource AppleUnit}" Text="ms"/>
                                </StackPanel>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="缩回延时"/>
                                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">
                                    <ctl:NumSliderBox Value="{Binding SelectedItem.RetractMs}" Min="0" Max="10000"/>
                                    <TextBlock Style="{StaticResource AppleUnit}" Text="ms"/>
                                </StackPanel>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="伸出速度"/>
                                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">
                                    <ctl:NumSliderBox Value="{Binding SelectedItem.ExtendSpeed}" Min="0" Max="100"/>
                                    <TextBlock Style="{StaticResource AppleUnit}" Text="%"/>
                                </StackPanel>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="缩回速度"/>
                                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">
                                    <ctl:NumSliderBox Value="{Binding SelectedItem.RetractSpeed}" Min="0" Max="100"/>
                                    <TextBlock Style="{StaticResource AppleUnit}" Text="%"/>
                                </StackPanel>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="到位容差"/>
                                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">
                                    <ctl:NumSliderBox Value="{Binding SelectedItem.ToleranceMs}" Min="0" Max="10000"/>
                                    <TextBlock Style="{StaticResource AppleUnit}" Text="ms"/>
                                </StackPanel>
                            </DockPanel>
                        </StackPanel>
                    </Border>

                </StackPanel>

                <!-- ============ 右列：IO配置 + 安全与逻辑 + 高级 ============ -->
                <StackPanel Grid.Row="1" Grid.Column="1" Margin="6,0,12,12" VerticalAlignment="Top">

                    <!-- IO 配置 -->
                    <Border Style="{StaticResource AppleGroupCard}">
                        <StackPanel>
                            <TextBlock Style="{StaticResource AppleSectionHeader}" Text="IO 配置"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="输出点"/>
                                <ComboBox DockPanel.Dock="Right" Style="{StaticResource AppleCombo}"
                                          ItemsSource="{x:Static svc:Catalog.IoNames}"
                                          SelectedItem="{Binding SelectedItem.OutPoint, UpdateSourceTrigger=PropertyChanged}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="伸出感应"/>
                                <ComboBox DockPanel.Dock="Right" Style="{StaticResource AppleCombo}"
                                          ItemsSource="{x:Static svc:Catalog.IoNames}"
                                          SelectedItem="{Binding SelectedItem.SensorExtend, UpdateSourceTrigger=PropertyChanged}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="缩回感应"/>
                                <ComboBox DockPanel.Dock="Right" Style="{StaticResource AppleCombo}"
                                          ItemsSource="{x:Static svc:Catalog.IoNames}"
                                          SelectedItem="{Binding SelectedItem.SensorRetract, UpdateSourceTrigger=PropertyChanged}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="感应类型"/>
                                <ListBox DockPanel.Dock="Right" ItemsSource="{Binding SensorTypeOptions}" SelectedItem="{Binding SelectedItem.SensorType, UpdateSourceTrigger=PropertyChanged}" Style="{StaticResource ApplePillList}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="备用感应"/>
                                <ComboBox DockPanel.Dock="Right" Style="{StaticResource AppleCombo}"
                                          ItemsSource="{x:Static svc:Catalog.IoNames}"
                                          SelectedItem="{Binding SelectedItem.BackupSensor, UpdateSourceTrigger=PropertyChanged}"/>
                            </DockPanel>
                        </StackPanel>
                    </Border>

                    <!-- 安全与逻辑 -->
                    <Border Style="{StaticResource AppleGroupCard}">
                        <StackPanel>
                            <TextBlock Style="{StaticResource AppleSectionHeader}" Text="安全与逻辑"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="互锁使能"/>
                                <CheckBox DockPanel.Dock="Right" Style="{StaticResource AppleToggle}" IsChecked="{Binding SelectedItem.Interlock}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="双线圈"/>
                                <CheckBox DockPanel.Dock="Right" Style="{StaticResource AppleToggle}" IsChecked="{Binding SelectedItem.DoubleCoil}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="报警使能"/>
                                <CheckBox DockPanel.Dock="Right" Style="{StaticResource AppleToggle}" IsChecked="{Binding SelectedItem.AlarmEnable}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="手动使能"/>
                                <CheckBox DockPanel.Dock="Right" Style="{StaticResource AppleToggle}" IsChecked="{Binding SelectedItem.ManualEnable}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="动作超时"/>
                                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">
                                    <ctl:NumSliderBox Value="{Binding SelectedItem.TimeoutMs}" Min="0" Max="10000"/>
                                    <TextBlock Style="{StaticResource AppleUnit}" Text="ms"/>
                                </StackPanel>
                            </DockPanel>
                        </StackPanel>
                    </Border>

                    <!-- 高级 -->
                    <Border Style="{StaticResource AppleGroupCard}">
                        <StackPanel>
                            <TextBlock Style="{StaticResource AppleSectionHeader}" Text="高级"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="脉冲输出"/>
                                <CheckBox DockPanel.Dock="Right" Style="{StaticResource AppleToggle}" IsChecked="{Binding SelectedItem.PulseOutput}"/>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="脉冲宽度"/>
                                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">
                                    <ctl:NumSliderBox Value="{Binding SelectedItem.PulseWidthMs}" Min="0" Max="10000"/>
                                    <TextBlock Style="{StaticResource AppleUnit}" Text="ms"/>
                                </StackPanel>
                            </DockPanel>
                            <Rectangle Style="{StaticResource AppleHairline}"/>
                            <DockPanel Style="{StaticResource AppleRow}">
                                <TextBlock DockPanel.Dock="Left" Style="{StaticResource AppleLabel}" Text="关联轴"/>
                                <ComboBox DockPanel.Dock="Right" Style="{StaticResource AppleCombo}"
                                          ItemsSource="{x:Static svc:Catalog.AxisNames}"
                                          SelectedItem="{Binding SelectedItem.LinkedAxis, UpdateSourceTrigger=PropertyChanged}"/>
                            </DockPanel>
                        </StackPanel>
                    </Border>

                </StackPanel>
            </Grid>
        </local:EditorPage.Detail>
    </local:EditorPage>
</UserControl>
'''

with open(p2, "w", encoding="utf-8", newline="\n") as f:
    f.write(header2)
    f.write(new_xaml)
    f.write(footer2)

print(f"  patched {p2}")
print("DONE")
