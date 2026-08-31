"""
完整重写 CylinderPage.xaml —— 保留前 3 行作者签名装饰符 + 完全重写主体。
之前 patch_cyl.py 残留了重复内容，必须清干净再写。
"""
import os

P = r"D:\wqz\code\NoCodeMotion\Views\CylinderPage.xaml"

# 1) 删掉旧文件
if os.path.exists(P):
    os.remove(P)
print(f"deleted {P}")

# 2) 写新文件（带作者签名装饰符头部 + 完整主体）
header = "// \u25c6\u25c7\u203b\u25e2\u25a4\u25a5\u25a6\u25a7\u25a8\u25a9\u2591\u2592\u2593\u2716\u2726\u2727\u2622\u2623\u27a4\u25c8\u2756\u25c6\u25c7\u203b\u25e2\u25a4\u25a5\u25a6\u25a7\u25a8\u25a9\u2591\u2592\u2593\u2716\u2726\u2727\u2622\u2623\u27a4\u25c8\u2756\u25c6\u25c7\u203b\u25e2\u25a4\u25a5\u25a6\u25a7\u25a8\u25a9\u2591\u2592\u2593\u2716\u2726\u2727\u200b\u2063\u200b\n// \u25c6\u6e29\u542f\u5fd7\u25c6\u7f16\u5199\u25c7\u5fae\u4fe1\uff5b187\u25c6\u31\u39\u33\u36\u25c7\u31\u33\u39\u39\u3000\u203b\u4fdd\u7559\u6240\u6709\u6743\u5229\u8bf7\u52ff\u5220\u9664\u25c1\u200b\u2063\u200b\n// \u25c6\u25c7\u203b\u25e2\u25a4\u25a5\u25a6\u25a7\u25a8\u25a9\u2591\u2592\u2593\u2716\u2726\u2727\u2622\u2623\u27a4\u25c8\u2756\u25c6\u25c7\u203b\u25e2\u25a4\u25a5\u25a6\u25a7\u25a8\u25a9\u2591\u2592\u2593\u2716\u2726\u2727\u200b\u2063\u200b\n"

body = '''<UserControl x:Class="NoCodeMotion.Views.CylinderPage"
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
        气缸页：左侧列表（每行内嵌"伸出 / 缩回"按钮）+ 右侧详情（仅配置表单）。
        手动动作按钮已从右侧详情移至左侧列表 —— 通过 EditorPage.LeftListItemTemplate 注入。
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
    </UserControl.Resources>

    <local:EditorPage>
        <!-- 注入列表项模板：让 EditorPage 用含内联按钮的版本 -->
        <local:EditorPage.LeftListItemTemplate>
            <DataTemplate>
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

footer = "\n// \u25c4\u4f5c\u8005\u4fdd\u7559\u6240\u6709\u6743\u5229\u3000\u8bf7\u52ff\u5220\u9664\u203b\u200b\u2063\u200b\n// \u25c6\u25c7\u203b\u25e2\u25a4\u25a5\u25a6\u25a7\u25a8\u25a9\u2591\u2592\u2593\u2716\u2726\u2727\u2622\u2623\u27a4\u25c8\u2756\u25c6\u25c7\u203b\u25e2\u25a4\u25a5\u25a6\u25a7\u25a8\u25a9\u2591\u2592\u2593\u2716\u2726\u2727\u200b\u2063\u200b\n"

with open(P, "w", encoding="utf-8", newline="\n") as f:
    f.write(header)
    f.write(body)
    f.write(footer)

print(f"wrote {P}  ({os.path.getsize(P)} bytes)")
