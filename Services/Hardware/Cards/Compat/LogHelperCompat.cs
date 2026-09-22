﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;

namespace Samsun.Domain.LogHelper.Model
{
    /// <summary>
    /// 日志模型（自参考工程 LogHelper/LogHelperModel.cs 逐字移植）。
    /// </summary>
    public class LogHelperModel
    {
        public long ID { get; set; }
        public int LogKindID { get; set; }
        public int Operator { get; set; }
        public string LogContent { get; set; }
        public DateTime AddTime { get; set; }
        public Exception exception { get; set; } = null;
    }
}

namespace Samsun.Domain.LogHelper.Helper
{
    // ────────────────────────────────────────────────────────────────
    // 日志出口兼容层（本工程自行实现，非厂商源码）
    //
    // 参考工程的 LogHelper 是 1273 行、基于 log4net 的日志框架，卡层只用到这里
    // 两个出口的 Warn 方法。NoCodeMotion 已有统一的硬件日志出口
    // （NoCodeMotion.Services.Hardware.HardwareLog），故转发到那里，
    // 不引入 log4net 依赖。
    //
    // 调用形态与参考实现一致：LogHelper.Helper.DefaultFileLogHelper.Warn(model)
    //   —— 其中 LogHelper 解析为命名空间 Samsun.Domain.LogHelper，
    //      Helper 是子命名空间，DefaultFileLogHelper 是该命名空间里的类型。
    // ────────────────────────────────────────────────────────────────

    /// <summary>日志出口的公共转发实现。</summary>
    internal static class LogSink
    {
        internal static void Write(string level, string channel, Model.LogHelperModel model)
        {
            if (model == null) return;
            string text = model.LogContent ?? string.Empty;
            if (model.exception != null)
                text += " 异常:" + model.exception.Message;
            NoCodeMotion.Services.Hardware.HardwareLog.Write(
                "[" + channel + "/" + level + "] " + text);
        }
    }

    /// <summary>默认文件日志（原：写 log4net 的 Default_File）。</summary>
    public static class DefaultFileLogHelper
    {
        public static void Debug(Model.LogHelperModel m) { LogSink.Write("DEBUG", "默认文件", m); }
        public static void Info(Model.LogHelperModel m) { LogSink.Write("INFO", "默认文件", m); }
        public static void Warn(Model.LogHelperModel m) { LogSink.Write("WARN", "默认文件", m); }
        public static void Error(Model.LogHelperModel m) { LogSink.Write("ERROR", "默认文件", m); }
    }

    /// <summary>调试消息窗口日志（原：写调试消息窗口）。</summary>
    public static class DebugMsgWindowLogHelper
    {
        public static void Debug(Model.LogHelperModel m) { LogSink.Write("DEBUG", "调试消息", m); }
        public static void Info(Model.LogHelperModel m) { LogSink.Write("INFO", "调试消息", m); }
        public static void Warn(Model.LogHelperModel m) { LogSink.Write("WARN", "调试消息", m); }
        public static void Error(Model.LogHelperModel m) { LogSink.Write("ERROR", "调试消息", m); }
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
