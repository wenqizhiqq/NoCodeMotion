// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧✦✧⚝☢☣➤◈❖◆◇※▣▤▥
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥▦▧✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧✦✧⚝☢☣➤◈❖◆◇※▣▤▥
using System;
using System.Globalization;
using System.Windows.Data;
using NoCodeMotion.Models;

namespace NoCodeMotion.Views
{
    /// <summary>流程状态 → 单字符/双字短标签（列表项右侧芯片文字）。
    /// Idle 直接返回空串（隐藏芯片不显示文字，详见 FlowStatusChipStyle 的默认 Collapsed）。</summary>
    public class FlowStatusLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FlowStatus st)
            {
                switch (st)
                {
                    case FlowStatus.Idle: return "";
                    case FlowStatus.Running: return "运行";     // 绿
                    case FlowStatus.Paused: return "暂停";        // 橙
                    case FlowStatus.Breakpoint: return "断点";   // 红
                    case FlowStatus.Exception: return "异常";    // 深红
                    case FlowStatus.Stopped: return "停止";      // 灰
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧✦✧⚝☢☣➤◈❖◆◇※▣▤▥
