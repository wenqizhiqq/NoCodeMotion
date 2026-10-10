// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace NoCodeMotion.Services.Hardware.Comm.Secs
{
    /// <summary>
    /// HSMS 消息类型（SEMI E37）。**注意：数据消息的 byte5 放的是功能号（Function），
    /// 控制消息的 byte5 才是下面这些控制码** —— 两者的取值区间重叠（SxF1 的功能号 1
    /// 和 Select.req 的 1 是同一个字节值），所以判定「是不是控制消息」必须靠
    /// 「byte3（Stream）== 0 且 byte5 落在控制码集合里」，不能只看 byte5。
    /// </summary>
    public enum HsmsSType : byte
    {
        /// <summary>数据消息（此时 byte5 = Function）。</summary>
        DataMessage = 0,
        /// <summary>Select.req —— 建立 HSMS 会话。</summary>
        SelectReq = 1,
        /// <summary>Select.rsp —— 会话建立应答（byte2 = Select 状态）。</summary>
        SelectRsp = 2,
        /// <summary>Deselect.req。</summary>
        DeselectReq = 3,
        /// <summary>Deselect.rsp。</summary>
        DeselectRsp = 4,
        /// <summary>Linktest.req —— 链路心跳。</summary>
        LinktestReq = 5,
        /// <summary>Linktest.rsp。</summary>
        LinktestRsp = 6,
        /// <summary>Reject.req（byte2 = 拒绝原因）。</summary>
        RejectReq = 7,
        /// <summary>Separate.req —— 断开 HSMS 会话。</summary>
        SeparateReq = 9,
    }

    /// <summary>
    /// 一条 HSMS 消息：10 字节头 + 可选 SECS-II 数据体。
    ///
    /// 头布局（大端）：
    ///   [0..1] SessionID —— 数据消息 = DeviceID；控制消息固定 0xFFFF
    ///   [2]    HeaderByte2 —— 数据消息 = W-bit（0x80）；控制消息 = 状态 / 拒绝原因
    ///   [3]    HeaderByte3 —— 数据消息 = Stream；控制消息 = 0
    ///   [4]    PType —— 固定 0（SECS-II）
    ///   [5]    SType —— 数据消息 = Function；控制消息 = 控制码
    ///   [6..9] SystemBytes —— 事务标识（大端）
    ///
    /// 文本侧（SML）：
    ///   数据：<c>S1F1 W</c>、<c>S2F41 W &lt;L &lt;A "START"&gt; &gt;</c>
    ///   控制：<c>SELECT</c> / <c>DESELECT</c> / <c>LINKTEST</c> / <c>SEPARATE</c>
    /// </summary>
    public sealed class SecsMessage
    {
        /// <summary>HSMS 头长度（固定 10 字节）。</summary>
        public const int HeaderLength = 10;

        /// <summary>控制消息用的会话号。</summary>
        public const ushort ControlSessionId = 0xFFFF;

        /// <summary>控制消息的 SType 取值集合（判定用）。</summary>
        public static readonly HsmsSType[] ControlTypes =
        {
            HsmsSType.SelectReq, HsmsSType.SelectRsp,
            HsmsSType.DeselectReq, HsmsSType.DeselectRsp,
            HsmsSType.LinktestReq, HsmsSType.LinktestRsp,
            HsmsSType.RejectReq, HsmsSType.SeparateReq,
        };

        /// <summary>消息类型（数据 / 各类控制）。</summary>
        public HsmsSType SType { get; set; } = HsmsSType.DataMessage;

        /// <summary>会话号：数据消息 = DeviceID；控制消息固定 0xFFFF。</summary>
        public ushort SessionId { get; set; }

        /// <summary>数据消息的流号（1~255）。</summary>
        public byte Stream { get; set; }

        /// <summary>数据消息的功能号（0~255）。</summary>
        public byte Function { get; set; }

        /// <summary>数据消息的 W-bit（true = 需要对方应答）。</summary>
        public bool WBit { get; set; }

        /// <summary>控制消息的 byte2（Select 状态 / Reject 原因）。</summary>
        public byte Status { get; set; }

        /// <summary>事务标识（同一事务的应答与请求相同）。</summary>
        public uint SystemBytes { get; set; }

        /// <summary>数据体根项；无数据体时为 null。</summary>
        public SecsItem Root { get; set; }

        /// <summary>是否数据消息。</summary>
        public bool IsData => SType == HsmsSType.DataMessage;

        /// <summary>是否控制消息。</summary>
        public bool IsControl => !IsData;

        /// <summary>数据消息的 DeviceID（等于会话号）。</summary>
        public ushort DeviceId
        {
            get => SessionId;
            set => SessionId = value;
        }

        // ---------------------------------------------------------------- 工厂

        /// <summary>构造数据消息。</summary>
        public static SecsMessage Data(ushort deviceId, int stream, int function, bool wBit, uint systemBytes, SecsItem root = null)
        {
            return new SecsMessage
            {
                SType = HsmsSType.DataMessage,
                SessionId = deviceId,
                Stream = (byte)stream,
                Function = (byte)function,
                WBit = wBit,
                SystemBytes = systemBytes,
                Root = root,
            };
        }

        /// <summary>构造控制消息（会话号固定 0xFFFF）。</summary>
        public static SecsMessage Control(HsmsSType type, byte status, uint systemBytes)
        {
            return new SecsMessage
            {
                SType = type,
                SessionId = ControlSessionId,
                Status = status,
                SystemBytes = systemBytes,
            };
        }

        private static int _systemByteSeed = 1;

        /// <summary>生成一个新的 SystemBytes（进程内单调递增，够用且不会重复）。</summary>
        public static uint NewSystemBytes()
        {
            int v = Interlocked.Increment(ref _systemByteSeed);
            return unchecked((uint)v);
        }

        /// <summary>对方是「请求」时，对应的应答类型；不适用返回 null。</summary>
        public HsmsSType? ReplyType()
        {
            switch (SType)
            {
                case HsmsSType.SelectReq: return HsmsSType.SelectRsp;
                case HsmsSType.DeselectReq: return HsmsSType.DeselectRsp;
                case HsmsSType.LinktestReq: return HsmsSType.LinktestRsp;
                default: return null;
            }
        }

        // ---------------------------------------------------------------- 编码

        /// <summary>只编码数据体（无体返回空数组）。</summary>
        public byte[] EncodeBody() => Root == null ? Array.Empty<byte>() : Root.ToBytes();

        /// <summary>编码为完整 HSMS 帧（10 字节头 + 数据体）。</summary>
        public byte[] Encode()
        {
            byte[] body = EncodeBody();
            var frame = new byte[HeaderLength + body.Length];

            frame[0] = (byte)((SessionId >> 8) & 0xFF);
            frame[1] = (byte)(SessionId & 0xFF);
            frame[2] = IsData ? (byte)(WBit ? 0x80 : 0x00) : Status;
            frame[3] = IsData ? Stream : (byte)0;
            frame[4] = 0;                                   // PType = SECS-II
            frame[5] = IsData ? Function : (byte)SType;
            frame[6] = (byte)((SystemBytes >> 24) & 0xFF);
            frame[7] = (byte)((SystemBytes >> 16) & 0xFF);
            frame[8] = (byte)((SystemBytes >> 8) & 0xFF);
            frame[9] = (byte)(SystemBytes & 0xFF);

            if (body.Length > 0) Array.Copy(body, 0, frame, HeaderLength, body.Length);
            return frame;
        }

        // ---------------------------------------------------------------- 解码

        /// <summary>
        /// 从 <paramref name="frame"/> 的 <paramref name="offset"/> 起解码一条消息，
        /// 帧长为 <paramref name="length"/>（含 10 字节头）。失败返回 null 并给出中文原因。
        /// </summary>
        public static SecsMessage Decode(byte[] frame, int offset, int length, out string error)
        {
            error = null;
            if (frame == null) { error = "帧为空"; return null; }
            if (length < HeaderLength) { error = "帧长度 " + length.ToString(CultureInfo.InvariantCulture) + " 不足 10 字节头"; return null; }
            if (offset < 0 || offset + length > frame.Length) { error = "帧越界（offset=" + offset.ToString(CultureInfo.InvariantCulture) + " length=" + length.ToString(CultureInfo.InvariantCulture) + "）"; return null; }

            int p = offset;
            ushort sid = (ushort)((frame[p] << 8) | frame[p + 1]);
            byte hb2 = frame[p + 2];
            byte hb3 = frame[p + 3];
            byte ptype = frame[p + 4];
            byte stype = frame[p + 5];
            uint sys = ((uint)frame[p + 6] << 24) | ((uint)frame[p + 7] << 16) | ((uint)frame[p + 8] << 8) | frame[p + 9];

            if (ptype != 0)
            {
                error = "PType=" + ptype.ToString(CultureInfo.InvariantCulture) + "（只支持 0 = SECS-II）";
                return null;
            }

            var msg = new SecsMessage { SystemBytes = sys, SessionId = sid };

            // ★ 控制消息判定：Stream(byte3) 必须为 0，且 byte5 落在控制码集合里。
            //   只看 byte5 会把 S1F1（功能号 1）误判成 Select.req。
            HsmsSType maybe = (HsmsSType)stype;
            bool isControl = hb3 == 0 && Array.IndexOf(ControlTypes, maybe) >= 0;

            if (isControl)
            {
                msg.SType = maybe;
                msg.Status = hb2;
                if (length > HeaderLength)
                {
                    error = "控制消息 " + STypeName(maybe) + " 不应带数据体（多出 "
                            + (length - HeaderLength).ToString(CultureInfo.InvariantCulture) + " 字节）";
                    return null;
                }
                return msg;
            }

            if ((hb2 & 0x7F) != 0)
            {
                error = "数据消息的 HeaderByte2=0x" + hb2.ToString("X2", CultureInfo.InvariantCulture) + " 非法（低 7 位必须为 0，只有最高位是 W-bit）";
                return null;
            }

            msg.SType = HsmsSType.DataMessage;
            msg.Stream = hb3;
            msg.Function = stype;
            msg.WBit = (hb2 & 0x80) != 0;

            if (length > HeaderLength)
            {
                int pos = offset + HeaderLength;
                var root = SecsItem.Decode(frame, ref pos, out error);
                if (root == null) { error = "数据体解码失败：" + error; return null; }
                if (pos != offset + length)
                {
                    error = "数据体解码后还剩 " + (offset + length - pos).ToString(CultureInfo.InvariantCulture) + " 字节没被消费（报文结构不对）";
                    return null;
                }
                msg.Root = root;
            }
            return msg;
        }

        // ---------------------------------------------------------------- 文本

        /// <summary>控制消息名（日志 / SML 用）。</summary>
        public static string STypeName(HsmsSType t)
        {
            switch (t)
            {
                case HsmsSType.DataMessage: return "DATA";
                case HsmsSType.SelectReq: return "SELECT.req";
                case HsmsSType.SelectRsp: return "SELECT.rsp";
                case HsmsSType.DeselectReq: return "DESELECT.req";
                case HsmsSType.DeselectRsp: return "DESELECT.rsp";
                case HsmsSType.LinktestReq: return "LINKTEST.req";
                case HsmsSType.LinktestRsp: return "LINKTEST.rsp";
                case HsmsSType.RejectReq: return "REJECT.req";
                case HsmsSType.SeparateReq: return "SEPARATE.req";
                default: return "?" + ((byte)t).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>消息头文本：数据消息 <c>S1F1 W</c>；控制消息 <c>SELECT.req</c>。</summary>
        public string HeaderText()
        {
            if (IsControl)
            {
                if (SType == HsmsSType.SelectRsp) return "SELECT.rsp(status=" + Status.ToString(CultureInfo.InvariantCulture) + ")";
                if (SType == HsmsSType.DeselectRsp) return "DESELECT.rsp(status=" + Status.ToString(CultureInfo.InvariantCulture) + ")";
                if (SType == HsmsSType.RejectReq) return "REJECT.req(reason=" + Status.ToString(CultureInfo.InvariantCulture) + ")";
                return STypeName(SType);
            }
            return "S" + Stream.ToString(CultureInfo.InvariantCulture)
                 + "F" + Function.ToString(CultureInfo.InvariantCulture)
                 + (WBit ? " W" : string.Empty);
        }

        /// <summary>多行 SML（头一行，数据体缩进）。</summary>
        public string ToSml()
        {
            if (Root == null) return HeaderText();
            return HeaderText() + "\n" + Root.ToSml();
        }

        /// <summary>单行 SML（日志一行一条）。</summary>
        public string ToSmlLine()
        {
            if (Root == null) return HeaderText();
            return HeaderText() + " " + Root.ToSmlLine();
        }

        public override string ToString() => ToSmlLine();

        /// <summary>
        /// 解析 SML 文本为消息：头（<c>S1F13 W</c> / <c>SELECT</c> / <c>LINKTEST</c> …）
        /// + 可选的一个数据项。失败返回 null 并给出中文原因。
        /// </summary>
        public static SecsMessage ParseSml(string text, ushort deviceId, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(text)) { error = "报文文本为空"; return null; }

            int pos = 0;
            SkipWs(text, ref pos);

            // 控制消息关键字
            string word = PeekWord(text, pos);
            var ctl = MatchControl(word);
            if (ctl != null)
            {
                pos += word.Length;
                SkipWs(text, ref pos);
                if (pos != text.Length)
                {
                    error = "控制消息「" + word + "」后面不应再有内容";
                    return null;
                }
                return Control(ctl.Value, 0, NewSystemBytes());
            }

            if (pos >= text.Length || (text[pos] != 'S' && text[pos] != 's'))
            {
                error = "报文应以 S1F1 这样的流/功能号开头（或 SELECT / DESELECT / LINKTEST / SEPARATE）";
                return null;
            }
            pos++;
            int stream = ReadInt(text, ref pos);
            if (stream < 0) { error = "S 后面缺少流号（如 S1F1）"; return null; }
            if (pos >= text.Length || (text[pos] != 'F' && text[pos] != 'f')) { error = "流号后面缺少 'F'（如 S1F1）"; return null; }
            pos++;
            int function = ReadInt(text, ref pos);
            if (function < 0) { error = "F 后面缺少功能号（如 S1F1）"; return null; }
            if (stream < 0 || stream > 255 || function < 0 || function > 255)
            {
                error = "流号/功能号必须在 0~255 之间";
                return null;
            }

            bool w = false;
            SkipWs(text, ref pos);
            if (pos < text.Length && (text[pos] == 'W' || text[pos] == 'w'))
            {
                w = true;
                pos++;
            }

            var msg = Data(deviceId, stream, function, w, NewSystemBytes());

            SkipWs(text, ref pos);
            if (pos < text.Length)
            {
                if (text[pos] != '<')
                {
                    error = "第 " + (pos + 1).ToString(CultureInfo.InvariantCulture) + " 个字符应为 '<'（数据体起始），实际是 '" + text[pos] + "'";
                    return null;
                }
                var root = SecsItem.ParseSml(text, ref pos, out error);
                if (root == null) return null;
                SkipWs(text, ref pos);
                if (pos != text.Length)
                {
                    error = "数据体之后还有多余内容：" + text.Substring(pos).Trim();
                    return null;
                }
                msg.Root = root;
            }
            return msg;
        }

        private static HsmsSType? MatchControl(string word)
        {
            switch (word.ToUpperInvariant())
            {
                case "SELECT": return HsmsSType.SelectReq;
                case "DESELECT": return HsmsSType.DeselectReq;
                case "LINKTEST": return HsmsSType.LinktestReq;
                case "SEPARATE": return HsmsSType.SeparateReq;
                default: return null;
            }
        }

        private static string PeekWord(string text, int pos)
        {
            int start = pos;
            while (pos < text.Length && char.IsLetter(text[pos])) pos++;
            return text.Substring(start, pos - start);
        }

        private static int ReadInt(string text, ref int pos)
        {
            int start = pos;
            while (pos < text.Length && char.IsDigit(text[pos])) pos++;
            if (pos == start) return -1;
            return int.Parse(text.Substring(start, pos - start), CultureInfo.InvariantCulture);
        }

        private static void SkipWs(string text, ref int pos)
        {
            while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
