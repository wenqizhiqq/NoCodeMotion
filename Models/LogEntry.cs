// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启​志​编​写​，​微​信​：​1​8​7​1​9​3​6​1​3​9​9　※保​留​所​有​权​利​请​勿​删​除◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;

namespace NoCodeMotion.Models
{
    /// <summary>异常日志等级：信息 / 警告 / 异常（错误）。</summary>
    public enum LogLevel
    {
        Info,
        Warn,
        Error
    }

    /// <summary>异常日志记录项：时间 + 等级 + 信息。供操作员页面的异常日志列表使用。</summary>
    public class LogEntry
    {
        /// <summary>发生时间。</summary>
        public DateTime Time { get; set; }

        /// <summary>日志等级（信息 / 警告 / 异常）。</summary>
        public LogLevel Level { get; set; }

        /// <summary>日志内容。</summary>
        public string Message { get; set; } = "";
    }
}
// ◇作​者​保​留​所​有​权​利　请​勿​删​除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
