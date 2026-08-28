// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Globalization;
using System.Windows.Data;
using NoCodeMotion.Services;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 把“功能”列的值映射到对应的名称库：轴→轴名称，IO→IO 名称，气缸→气缸名称，modbus→通讯名称。
    /// 用于流程「名称」下拉框按功能只显示同类已配置对象。
    /// </summary>
    public class FunctionToNamesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                "轴" => Catalog.AxisNames,
                "IO" => Catalog.IoNames,
                "气缸" => Catalog.CylinderNames,
                "modbus" => Catalog.CommNames,
                "变量" => Catalog.VariableNames,
                "点位" => Catalog.PointNames,
                _ => Catalog.AllNames
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
