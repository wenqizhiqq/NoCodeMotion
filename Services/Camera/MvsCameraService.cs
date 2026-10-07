// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁠志‎◆‌编‍写​◇⁠微⁠信⁣﹕⁣1⁠8‎7‎◆‍1​9‎3​6⁠◇‍1‌3⁠9⁣9⁣　‌※⁣保⁣留‌所​有⁣权⁠利​请⁠勿​删‏除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using MvCamCtrl.NET;

namespace NoCodeMotion.Services.Camera
{
    /// <summary>枚举到的一台真实相机（海康 MVS）。</summary>
    public sealed class MvsCameraDevice
    {
        /// <summary>枚举序号（本次枚举内的下标）。</summary>
        public int Index { get; init; }

        /// <summary>传输层：GigE / USB3 / CameraLink / 未知。</summary>
        public string LayerName { get; init; } = "未知";

        /// <summary>MVS 的传输层位标志（MV_GIGE_DEVICE / MV_USB_DEVICE）。</summary>
        public uint LayerType { get; init; }

        /// <summary>型号（如 MV-CE060-10GC）。</summary>
        public string Model { get; init; } = string.Empty;

        /// <summary>序列号（同型号多台时唯一，用作会话缓存键）。</summary>
        public string Serial { get; init; } = string.Empty;

        /// <summary>用户自定义名称（相机内可改）。</summary>
        public string UserName { get; init; } = string.Empty;

        /// <summary>当前 IP（仅 GigE；USB 相机为空）。</summary>
        public string Ip { get; init; } = string.Empty;

        /// <summary>MVS 原始设备信息（CreateDevice 时要原样传回）。</summary>
        internal MyCamera.MV_CC_DEVICE_INFO Info { get; init; }

        /// <summary>会话缓存键：优先序列号，其次 IP，最后型号+序号。</summary>
        internal string Key =>
            !string.IsNullOrWhiteSpace(Serial) ? "SN:" + Serial
            : !string.IsNullOrWhiteSpace(Ip) ? "IP:" + Ip
            : "IDX:" + LayerName + ":" + Index;

        /// <summary>界面上显示的一行描述。</summary>
        public string Display
        {
            get
            {
                var name = !string.IsNullOrWhiteSpace(UserName) ? UserName
                         : !string.IsNullOrWhiteSpace(Model) ? Model
                         : LayerName + " 相机";
                var tail = !string.IsNullOrWhiteSpace(Ip) ? Ip
                         : !string.IsNullOrWhiteSpace(Serial) ? Serial
                         : "—";
                return $"{name}（{LayerName} / {tail}）";
            }
        }

        public override string ToString() => Display;
    }

    /// <summary>打开相机时的参数。</summary>
    public sealed class MvsCameraSettings
    {
        /// <summary>曝光时间（毫秒）。</summary>
        public double ExposureMs { get; set; } = 10.0;

        /// <summary>增益（dB，1.0 = 1 倍）。</summary>
        public double Gain { get; set; } = 1.0;

        /// <summary>触发模式：连续 / 软触发 / 硬触发。</summary>
        public string TriggerMode { get; set; } = "连续";

        /// <summary>单帧取流超时（毫秒）。</summary>
        public int GrabTimeoutMs { get; set; } = 1000;
    }

    /// <summary>
    /// 海康 MVS 相机服务（真实取像）。
    ///
    /// 依赖两层：
    ///   · 托管封装 <c>Native\MvCameraControl.Net.dll</c>（随本程序发布，AnyCPU 纯 IL，可在 .NET 10 运行）；
    ///   · 原生 <c>MvCameraControl.dll</c>（由**目标机安装的 MVS 运行库**提供，本程序不随附）。
    ///
    /// 原生库缺失时（例如开发机只装了 MVS 的 Development 目录、没装 Runtime），所有调用会抛
    /// <see cref="DllNotFoundException"/>。本类把这种情况统一收敛成
    /// <see cref="IsRuntimeAvailable"/>=false + <see cref="LastError"/> 文案，
    /// 让相机页与流程视觉**优雅降级**（回退仿真），而不是崩掉整个程序。
    ///
    /// 会话共享：一台 GigE 相机同一时刻只能被一个句柄独占打开。相机页与流程视觉若各自开一次，
    /// 第二次会拿到"设备被占用"。所以这里按设备键缓存**唯一一个**已打开会话，两边共用。
    /// </summary>
    public static class MvsCameraService
    {
        private static readonly object _gate = new();

        /// <summary>会话缓存：设备键 → 已打开的相机。</summary>
        private static readonly Dictionary<string, MvsCameraSession> _sessions = new();

        private static bool? _runtimeOk;
        private static string _lastError = string.Empty;

        /// <summary>最近一次失败原因（空 = 无错误）。</summary>
        public static string LastError
        {
            get { lock (_gate) return _lastError; }
        }

        /// <summary>
        /// MVS 运行库是否可用（首次调用会真枚举一次设备并缓存结果）。
        /// 托管封装能加载、但原生 MvCameraControl.dll 缺失时返回 false。
        /// </summary>
        public static bool IsRuntimeAvailable
        {
            get
            {
                lock (_gate)
                {
                    if (_runtimeOk.HasValue) return _runtimeOk.Value;
                    try
                    {
                        var list = new MyCamera.MV_CC_DEVICE_INFO_LIST();
                        int rc = MyCamera.MV_CC_EnumDevices_NET(MvLayerAll, ref list);
                        _runtimeOk = rc == MyCamera.MV_OK;
                        if (_runtimeOk != true) _lastError = Describe(rc);
                    }
                    catch (DllNotFoundException ex)
                    {
                        _runtimeOk = false;
                        _lastError = "未找到原生 MvCameraControl.dll，请先安装海康 MVS 运行库（Runtime）。" + ex.Message;
                    }
                    catch (BadImageFormatException ex)
                    {
                        _runtimeOk = false;
                        _lastError = "MvCameraControl.dll 位数与进程不匹配（需 64 位）。" + ex.Message;
                    }
                    catch (Exception ex)
                    {
                        _runtimeOk = false;
                        _lastError = $"{ex.GetType().Name}：{ex.Message}";
                    }
                    return _runtimeOk.Value;
                }
            }
        }

        private const uint MvLayerAll = (uint)(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE);

        /// <summary>枚举当前在线的相机（GigE + USB3）。失败返回空列表，原因见 <see cref="LastError"/>。</summary>
        public static List<MvsCameraDevice> Enumerate()
        {
            var result = new List<MvsCameraDevice>();
            lock (_gate)
            {
                try
                {
                    var list = new MyCamera.MV_CC_DEVICE_INFO_LIST();
                    int rc = MyCamera.MV_CC_EnumDevices_NET(MvLayerAll, ref list);
                    if (rc != MyCamera.MV_OK)
                    {
                        _runtimeOk = false;
                        _lastError = Describe(rc);
                        return result;
                    }
                    _runtimeOk = true;

                    if (list.pDeviceInfo == null) return result;
                    int n = (int)Math.Min(list.nDeviceNum, (uint)list.pDeviceInfo.Length);
                    for (int i = 0; i < n; i++)
                    {
                        var ptr = list.pDeviceInfo[i];
                        if (ptr == IntPtr.Zero) continue;
                        var dev = ReadDevice(ptr, i);
                        if (dev != null) result.Add(dev);
                    }
                    _lastError = result.Count == 0 ? "未发现相机（请检查网线 / 供电 / MVS 客户端能否看到设备）。" : string.Empty;
                }
                catch (DllNotFoundException ex)
                {
                    _runtimeOk = false;
                    _lastError = "未找到原生 MvCameraControl.dll，请先安装海康 MVS 运行库（Runtime）。" + ex.Message;
                }
                catch (Exception ex)
                {
                    _runtimeOk = false;
                    _lastError = $"{ex.GetType().Name}：{ex.Message}";
                }
            }
            return result;
        }

        /// <summary>把 MVS 原始设备信息解析成可读的 <see cref="MvsCameraDevice"/>。</summary>
        private static MvsCameraDevice? ReadDevice(IntPtr ptr, int index)
        {
            var info = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                ptr, typeof(MyCamera.MV_CC_DEVICE_INFO))!;

            if (info.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                var gige = PtrToStructFromFixedBuffer<MyCamera.MV_GIGE_DEVICE_INFO>(info.SpecialInfo.stGigEInfo);
                return new MvsCameraDevice
                {
                    Index = index,
                    LayerType = MyCamera.MV_GIGE_DEVICE,
                    LayerName = "GigE",
                    Model = Trim(gige.chModelName),
                    Serial = Trim(gige.chSerialNumber),
                    UserName = Trim(gige.chUserDefinedName),
                    Ip = IpOf(gige.nCurrentIp),
                    Info = info
                };
            }

            if (info.nTLayerType == MyCamera.MV_USB_DEVICE)
            {
                var usb = PtrToStructFromFixedBuffer<MyCamera.MV_USB3_DEVICE_INFO>(info.SpecialInfo.stUsb3VInfo);
                return new MvsCameraDevice
                {
                    Index = index,
                    LayerType = MyCamera.MV_USB_DEVICE,
                    LayerName = "USB3",
                    Model = Trim(usb.chModelName),
                    Serial = Trim(usb.chSerialNumber),
                    UserName = Trim(usb.chUserDefinedName),
                    Ip = string.Empty,
                    Info = info
                };
            }

            return new MvsCameraDevice
            {
                Index = index,
                LayerType = info.nTLayerType,
                LayerName = "其它",
                Info = info
            };
        }

        /// <summary>
        /// 把 SPECIAL_INFO 里的定长字节数组（union）解释成具体设备信息结构体。
        /// 必须 Pin 住数组再取地址——它是托管堆上的数组，不 Pin 会被 GC 搬走。
        /// </summary>
        private static T PtrToStructFromFixedBuffer<T>(byte[] buffer) where T : struct
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T))!;
            }
            finally
            {
                handle.Free();
            }
        }

        private static string IpOf(uint ip)
        {
            try { return new IPAddress(ip).ToString(); }
            catch { return string.Empty; }
        }

        private static string Trim(string? s) => (s ?? string.Empty).Trim('\0', ' ');

        /// <summary>
        /// 取一帧真实图像（BGRA，可直接喂给 WPF BitmapSource）。
        /// 相机未连接会自动打开；任何失败都返回 false 并把原因写进 <paramref name="error"/>。
        /// </summary>
        public static bool TryGrabBgra(MvsCameraDevice device, MvsCameraSettings settings,
                                       out byte[]? bgra, out int width, out int height, out string error)
        {
            bgra = null; width = 0; height = 0; error = string.Empty;
            if (device == null) { error = "未指定相机。"; return false; }

            MvsCameraSession session;
            lock (_gate)
            {
                if (!_sessions.TryGetValue(device.Key, out var cached) || !cached.IsOpen)
                {
                    cached?.Dispose();
                    var opened = MvsCameraSession.Open(device, settings, out var openErr);
                    if (opened == null)
                    {
                        _sessions.Remove(device.Key);
                        error = openErr;
                        _lastError = openErr;
                        return false;
                    }
                    _sessions[device.Key] = opened;
                }
                session = _sessions[device.Key];
            }

            if (!session.GrabBgra(settings.GrabTimeoutMs, out bgra, out width, out height, out error))
            {
                lock (_gate) _lastError = error;
                return false;
            }
            return true;
        }

        /// <summary>相机是否已打开（供界面显示连接状态）。</summary>
        public static bool IsOpen(string deviceKey)
        {
            lock (_gate) return _sessions.TryGetValue(deviceKey, out var s) && s.IsOpen;
        }

        /// <summary>关闭指定设备的会话。</summary>
        public static void Close(string deviceKey)
        {
            lock (_gate)
            {
                if (_sessions.TryGetValue(deviceKey, out var s))
                {
                    s.Dispose();
                    _sessions.Remove(deviceKey);
                }
            }
        }

        /// <summary>关闭全部会话（程序退出 / 断开所有相机时调用）。</summary>
        public static void CloseAll()
        {
            lock (_gate)
            {
                foreach (var s in _sessions.Values) s.Dispose();
                _sessions.Clear();
            }
        }

        /// <summary>把 MVS 返回码翻译成可读文案。</summary>
        public static string Describe(int rc)
        {
            if (rc == MyCamera.MV_OK) return "成功";
            if (rc == MyCamera.MV_E_HANDLE) return "句柄无效或为空。";
            if (rc == MyCamera.MV_E_SUPPORT) return "该相机不支持此功能。";
            if (rc == MyCamera.MV_E_CALLORDER) return "接口调用顺序错误。";
            if (rc == MyCamera.MV_E_PARAMETER) return "参数错误。";
            if (rc == MyCamera.MV_E_RESOURCE) return "申请资源失败（内存 / 句柄不足）。";
            if (rc == MyCamera.MV_E_NODATA) return "没有数据（超时未取到帧）。";
            if (rc == MyCamera.MV_E_PRECONDITION) return "运行环境变化或前置条件不满足。";
            if (rc == MyCamera.MV_E_NOENOUGH_BUF) return "传入缓冲区不足。";
            if (rc == MyCamera.MV_E_ABNORMAL_IMAGE) return "图像异常（可能丢包导致不完整）。";
            if (rc == MyCamera.MV_E_LOAD_LIBRARY) return "加载库失败（缺原生 MvCameraControl.dll？）。";
            if (rc == MyCamera.MV_E_NOOUTBUF) return "没有可用输出缓冲区。";
            return string.Format(CultureInfo.InvariantCulture, "MVS 错误码 0x{0:X8}", rc);
        }

        /// <summary>把相机项的"厂商/型号/IP"与枚举结果对上（供相机页与流程按索引定位真实设备）。</summary>
        public static MvsCameraDevice? Match(List<MvsCameraDevice> devices, string? serialOrIp, int fallbackIndex)
        {
            if (devices == null || devices.Count == 0) return null;
            if (!string.IsNullOrWhiteSpace(serialOrIp))
            {
                var key = serialOrIp.Trim();
                foreach (var d in devices)
                    if (string.Equals(d.Serial, key, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(d.Ip, key, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(d.UserName, key, StringComparison.OrdinalIgnoreCase))
                        return d;
            }
            if (fallbackIndex >= 0 && fallbackIndex < devices.Count) return devices[fallbackIndex];
            return devices[0];
        }
    }

    /// <summary>一台已打开的相机。独占设备，用完必须 <see cref="Dispose"/>。</summary>
    public sealed class MvsCameraSession : IDisposable
    {
        private readonly MyCamera _cam = new();
        private IntPtr _buffer = IntPtr.Zero;
        private int _bufferSize;
        private bool _opened;
        private bool _grabbing;
        private bool _disposed;

        public MvsCameraDevice Device { get; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        public bool IsOpen => _opened && !_disposed;

        private MvsCameraSession(MvsCameraDevice device) => Device = device;

        /// <summary>创建句柄 → 打开设备 → 下发参数 → 开始取流。任一步失败返回 null 并给出原因。</summary>
        public static MvsCameraSession? Open(MvsCameraDevice device, MvsCameraSettings settings, out string error)
        {
            error = string.Empty;
            var s = new MvsCameraSession(device);
            try
            {
                var info = device.Info;   // CreateDevice 要求传回原始结构体
                int rc = s._cam.MV_CC_CreateDevice_NET(ref info);
                if (rc != MyCamera.MV_OK) { error = "创建相机句柄失败：" + MvsCameraService.Describe(rc); return null; }

                rc = s._cam.MV_CC_OpenDevice_NET();
                if (rc != MyCamera.MV_OK)
                {
                    error = "打开相机失败：" + MvsCameraService.Describe(rc)
                          + "（若相机已被 MVS 客户端或本程序其它位置打开，请先关闭）";
                    s._cam.MV_CC_DestroyDevice_NET();
                    return null;
                }
                s._opened = true;

                // GigE：按 SDK 建议设置最优包长，否则高分辨率下容易丢包。
                if (device.LayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int packet = s._cam.MV_CC_GetOptimalPacketSize_NET();
                    if (packet > 0) s._cam.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)packet);
                }

                // 读一次分辨率，用来申请取流缓冲。
                s.Width = s.ReadInt("Width");
                s.Height = s.ReadInt("Height");
                if (s.Width <= 0 || s.Height <= 0) { s.Width = 1920; s.Height = 1080; }

                s.ApplySettings(settings);

                s._cam.MV_CC_SetImageNodeNum_NET(3);   // 缓冲 3 帧，降低丢帧概率
                rc = s._cam.MV_CC_StartGrabbing_NET();
                if (rc != MyCamera.MV_OK)
                {
                    error = "开始取流失败：" + MvsCameraService.Describe(rc);
                    s.Dispose();
                    return null;
                }
                s._grabbing = true;

                // BGR8_Packed = 3 字节/像素；留 4 字节/像素的余量以防某些相机直接回 BGRA。
                s._bufferSize = s.Width * s.Height * 4;
                s._buffer = Marshal.AllocHGlobal(s._bufferSize);
                return s;
            }
            catch (Exception ex)
            {
                error = $"{ex.GetType().Name}：{ex.Message}";
                s.Dispose();
                return null;
            }
        }

        private int ReadInt(string key)
        {
            try
            {
                var v = new MyCamera.MVCC_INTVALUE();
                return _cam.MV_CC_GetIntValue_NET(key, ref v) == MyCamera.MV_OK ? (int)v.nCurValue : 0;
            }
            catch { return 0; }
        }

        /// <summary>下发曝光 / 增益 / 触发模式。单项失败不致命，只记原因。</summary>
        public void ApplySettings(MvsCameraSettings settings)
        {
            if (!IsOpen) return;
            try
            {
                // 曝光：SDK 单位是微秒，界面单位是毫秒。
                int rc = _cam.MV_CC_SetExposureTime_NET((float)(settings.ExposureMs * 1000.0));
                if (rc != MyCamera.MV_OK) LastError = "设置曝光失败：" + MvsCameraService.Describe(rc);

                _cam.MV_CC_SetEnumValueByString_NET("ExposureAuto", "Off");   // 先关自动曝光，否则手动值被覆盖
                _cam.MV_CC_SetEnumValueByString_NET("GainAuto", "Off");
                _cam.MV_CC_SetGain_NET((float)settings.Gain);

                switch (settings.TriggerMode)
                {
                    case "软触发":
                        _cam.MV_CC_SetEnumValueByString_NET("TriggerSource", "Software");
                        _cam.MV_CC_SetEnumValueByString_NET("TriggerMode", "On");
                        break;
                    case "硬触发":
                        _cam.MV_CC_SetEnumValueByString_NET("TriggerSource", "Line0");
                        _cam.MV_CC_SetEnumValueByString_NET("TriggerMode", "On");
                        break;
                    default:   // 连续
                        _cam.MV_CC_SetEnumValueByString_NET("TriggerMode", "Off");
                        break;
                }
            }
            catch (Exception ex)
            {
                LastError = $"{ex.GetType().Name}：{ex.Message}";
            }
        }

        /// <summary>软触发一次（软触发模式下必须先调它才会出图）。</summary>
        public bool SoftTrigger()
        {
            if (!IsOpen) return false;
            try { return _cam.MV_CC_SetCommandValue_NET("TriggerSoftware") == MyCamera.MV_OK; }
            catch { return false; }
        }

        /// <summary>
        /// 取一帧并转成 BGRA。内部按返回帧长判断真实像素格式（BGR8 / BGRA8 / Mono8），
        /// 不依赖像素格式常量，兼容性更好。
        /// </summary>
        public bool GrabBgra(int timeoutMs, out byte[]? bgra, out int width, out int height, out string error)
        {
            bgra = null; width = 0; height = 0; error = string.Empty;
            if (!IsOpen) { error = "相机未打开。"; return false; }
            if (!_grabbing) { error = "相机未开始取流。"; return false; }

            try
            {
                var info = new MyCamera.MV_FRAME_OUT_INFO_EX();
                int rc = _cam.MV_CC_GetImageForBGR_NET(_buffer, (uint)_bufferSize, ref info, timeoutMs);
                if (rc != MyCamera.MV_OK)
                {
                    error = "取帧失败：" + MvsCameraService.Describe(rc);
                    return false;
                }

                int w = info.nWidth, h = info.nHeight;
                if (w <= 0 || h <= 0) { error = "取到的帧尺寸无效。"; return false; }

                int len = (int)info.nFrameLen;
                int px = w * h;
                var src = new byte[len > 0 ? len : px * 3];
                Marshal.Copy(_buffer, src, 0, src.Length);

                bgra = ToBgra(src, w, h, len);
                width = w; height = h;
                return true;
            }
            catch (Exception ex)
            {
                error = $"{ex.GetType().Name}：{ex.Message}";
                return false;
            }
        }

        /// <summary>按帧长推断像素布局并展开为 BGRA。</summary>
        private static byte[] ToBgra(byte[] src, int w, int h, int frameLen)
        {
            int px = w * h;
            var dst = new byte[px * 4];

            // BGR8_Packed（3 字节/像素）是 MV_CC_GetImageForBGR 的标准输出
            if (frameLen >= px * 3 && frameLen < px * 4)
            {
                for (int i = 0, s = 0, d = 0; i < px; i++, s += 3, d += 4)
                {
                    dst[d] = src[s];        // B
                    dst[d + 1] = src[s + 1];// G
                    dst[d + 2] = src[s + 2];// R
                    dst[d + 3] = 255;       // A
                }
                return dst;
            }

            // BGRA8_Packed（4 字节/像素）
            if (frameLen >= px * 4)
            {
                Buffer.BlockCopy(src, 0, dst, 0, dst.Length);
                return dst;
            }

            // 单色（1 字节/像素）——保险起见也支持
            for (int i = 0; i < px; i++)
            {
                byte g = i < src.Length ? src[i] : (byte)0;
                int d = i * 4;
                dst[d] = g; dst[d + 1] = g; dst[d + 2] = g; dst[d + 3] = 255;
            }
            return dst;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_grabbing) { _cam.MV_CC_StopGrabbing_NET(); _grabbing = false; }
                if (_opened) { _cam.MV_CC_CloseDevice_NET(); _opened = false; }
                try { _cam.MV_CC_DestroyDevice_NET(); } catch { /* 句柄已销毁 */ }
            }
            catch { /* 退出路径不抛 */ }
            finally
            {
                if (_buffer != IntPtr.Zero) { Marshal.FreeHGlobal(_buffer); _buffer = IntPtr.Zero; }
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​

