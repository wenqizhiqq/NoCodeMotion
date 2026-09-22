﻿// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温⁠启‎志‎◆‎编⁠写​◇⁣微‌信⁠﹕‍1‎8‎7‌◆‏1​9‌3‍6‍◇‏1‌3‌9‏9‌　​※⁣保‌留‏所‏有⁣权‏利⁠请⁠勿‏删‎除‎◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ────────────────────────────────────────────────────────────────
// 移植自 SamsunMotion / SMotorse 运动控制卡层参考实现
//   源: MotionCardRes/Tools/HPTimer.cs
//   改动: 仅行尾归一化为 LF、加 #nullable disable；命名空间与逻辑保持原样。
// ────────────────────────────────────────────────────────────────
#nullable disable
using System;
using System.Runtime.InteropServices;

namespace Samsun.Domain.MotionCard.Tools.HighPrecisionTimer
{
    /// <summary>
    /// 定时器分辨率：毫秒
    /// </summary>
    struct TimerCaps
    {
        /// <summary>最小周期</summary>
        public int periodMin;
        /// <summary>最大周期</summary>
        public int periodMax;
    }

    /// <summary>
    /// 高精度定时器
    /// </summary>
    public class HPTimer
    {
        static HPTimer()
        {
            TimeGetDevCaps(ref _caps, Marshal.SizeOf(_caps));
        }

        public HPTimer()
        {
            Running = false;
            _interval = _caps.periodMin;
            _resolution = _caps.periodMin;
            _callback = new TimerCallback(TimerEventCallback);
        }

        ~HPTimer()
        {
            TimeKillEvent(_id);
        }

        /// <summary>
        /// 系统定时器回调
        /// </summary>
        /// <param name="id">定时器编号</param>
        /// <param name="msg">预留，不使用</param>
        /// <param name="user">用户实例数据</param>
        /// <param name="param1">预留，不使用</param>
        /// <param name="param2">预留，不使用</param>
        private delegate void TimerCallback(int id, int msg, int user, int param1, int param2);

        #region 动态库接口

        /// <summary>
        /// 查询设备支持的定时器分辨率
        /// </summary>
        /// <param name="ptc">定时器分辨率结构体指针</param>
        /// <param name="cbtc">定时器分辨率结构体大小</param>
        /// <returns></returns>
        [DllImport("winmm.dll", EntryPoint = "timeGetDevCaps")]
        private static extern TimerError TimeGetDevCaps(ref TimerCaps ptc, int cbtc);

        /// <summary>
        /// 绑定定时器事件
        /// </summary>
        /// <param name="delay">延时：毫秒</param>
        /// <param name="resolution">分辨率</param>
        /// <param name="callback">回调接口</param>
        /// <param name="user">用户提供的回调数据</param>
        /// <param name="eventType"></param>
        [DllImport("winmm.dll", EntryPoint = "timeSetEvent")]
        private static extern int TimeSetEvent(int delay, int resolution, TimerCallback callback, int user, int eventType);

        /// <summary>
        /// 终止定时器
        /// </summary>
        /// <param name="id">定时器编号</param>
        [DllImport("winmm.dll", EntryPoint = "timeKillEvent")]
        private static extern TimerError TimeKillEvent(int id);

        #endregion

        #region 属性

        /// <summary>时间间隔：毫秒</summary>
        public int Interval
        {
            get { return _interval; }
            set
            {
                if (value < _caps.periodMin || value > _caps.periodMax)
                    throw new Exception("无效的计时间隔");
                _interval = value;
            }
        }

        public bool Running { get; private set; }

        #endregion

        #region 事件

        public event Action Ticked;

        #endregion

        #region 公开方法

        public void Start()
        {
            if (!Running)
            {
                _id = TimeSetEvent(_interval, _resolution, _callback, 0,
                    (int)EventType01.TIME_PERIODIC | (int)EventType02.TIME_KILL_SYNCHRONOUS);
                if (_id == 0) throw new Exception("启动定时器失败");
                Running = true;
            }
        }

        public void Stop()
        {
            if (Running)
            {
                TimeKillEvent(_id);
                Running = false;
            }
        }

        #endregion

        #region 内部方法

        private void TimerEventCallback(int id, int msg, int user, int param1, int param2)
        {
            Ticked?.Invoke();
        }

        #endregion

        #region 字段

        // 系统定时器分辨率
        private static TimerCaps _caps;
        // 定时器间隔
        private int _interval;
        // 定时器分辨率
        private int _resolution;
        // 定时器回调
        private TimerCallback _callback;
        // 定时器编号
        private int _id;

        #endregion
    }
}
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
