#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// 一条真实通讯通道（串口 / 网口 TCP / 网口 UDP / Modbus RTU / Modbus TCP）。
    /// 由 <see cref="CommManager"/> 按项目里的「通讯」配置创建与缓存。
    /// </summary>
    public interface ICommChannel : IDisposable
    {
        /// <summary>通道名称（等于配置里的 Name）。</summary>
        string Name { get; }

        /// <summary>是否已连接 / 已打开。</summary>
        bool IsOpen { get; }

        /// <summary>打开连接（重复调用安全）。</summary>
        void Open();

        /// <summary>发送数据。文本直接发；<c>HEX:</c> 前缀按十六进制字节发；Modbus 通道按命令语法执行。</summary>
        void Send(string data);

        /// <summary>接收一段数据（超时按配置的 TimeoutMs）。没有数据返回空串。</summary>
        string Recv();
    }

    /// <summary>
    /// 收发内容的编解码：让 Lua 里既能发文本，也能发十六进制字节。
    ///
    /// 发送（<see cref="Encode"/>）：
    ///   - <c>"Trigger\r\n"</c>       → 文本，支持 \r \n \t \\ \xNN 转义
    ///   - <c>"HEX:02 41 42 03"</c>   → 按十六进制字节发送（允许空格 / 逗号 / 0x 前缀）
    ///
    /// 接收（<see cref="Decode"/>）：
    ///   - 全是可见字符 → 直接返回文本（去掉尾部换行）
    ///   - 含不可见字节 → 返回 <c>"HEX:xx xx .."</c>，便于脚本里比较
    /// </summary>
    internal static class PayloadCodec
    {
        public const string HexPrefix = "HEX:";

        /// <summary>把 Lua 传来的字符串编码成要发送的字节。</summary>
        public static byte[] Encode(string data)
        {
            if (string.IsNullOrEmpty(data)) return Array.Empty<byte>();

            if (data.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase))
                return ParseHex(data.Substring(HexPrefix.Length));

            return Encoding.UTF8.GetBytes(Unescape(data));
        }

        /// <summary>把收到的字节解码成 Lua 好处理的字符串。</summary>
        public static string Decode(byte[] buffer, int length)
        {
            if (buffer == null || length <= 0) return string.Empty;

            bool printable = true;
            for (int i = 0; i < length; i++)
            {
                byte b = buffer[i];
                bool ok = b == 0x09 || b == 0x0A || b == 0x0D || (b >= 0x20 && b <= 0x7E) || b >= 0x80;
                if (!ok) { printable = false; break; }
            }

            if (printable)
            {
                string text = Encoding.UTF8.GetString(buffer, 0, length);
                if (!text.Contains('\uFFFD'))
                    return text.TrimEnd('\r', '\n');
            }

            var sb = new StringBuilder(HexPrefix, HexPrefix.Length + length * 3);
            for (int i = 0; i < length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(buffer[i].ToString("X2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>解析十六进制串："02 41 0x42,03" → 字节数组。</summary>
        public static byte[] ParseHex(string hex)
        {
            var bytes = new List<byte>();
            var token = new StringBuilder(2);
            for (int i = 0; i < hex.Length; i++)
            {
                char c = hex[i];
                if (c == ' ' || c == ',' || c == '-' || c == '\t')
                {
                    FlushToken(token, bytes);
                    continue;
                }
                if (c == '0' && i + 1 < hex.Length && (hex[i + 1] == 'x' || hex[i + 1] == 'X'))
                {
                    FlushToken(token, bytes);
                    i++;
                    continue;
                }
                if (!Uri.IsHexDigit(c))
                    throw new ArgumentException($"十六进制数据里有非法字符 '{c}'：{hex}");

                token.Append(c);
                if (token.Length == 2) FlushToken(token, bytes);
            }
            FlushToken(token, bytes);
            return bytes.ToArray();
        }

        private static void FlushToken(StringBuilder token, List<byte> bytes)
        {
            if (token.Length == 0) return;
            bytes.Add(byte.Parse(token.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            token.Clear();
        }

        /// <summary>处理 \r \n \t \\ \xNN 转义。</summary>
        public static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\\' || i == s.Length - 1) { sb.Append(s[i]); continue; }
                char n = s[++i];
                switch (n)
                {
                    case 'r': sb.Append('\r'); break;
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case '0': sb.Append('\0'); break;
                    case '\\': sb.Append('\\'); break;
                    case 'x':
                    case 'X':
                        if (i + 2 < s.Length &&
                            byte.TryParse(s.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hv))
                        {
                            sb.Append((char)hv);
                            i += 2;
                        }
                        else sb.Append(n);
                        break;
                    default: sb.Append('\\').Append(n); break;
                }
            }
            return sb.ToString();
        }
    }
}
