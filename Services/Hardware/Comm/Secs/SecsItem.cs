// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NoCodeMotion.Services.Hardware.Comm.Secs
{
    /// <summary>
    /// SECS-II 数据项格式码。**这里的数值就是 SEMI E5 里那个 6 位格式码本身**
    /// （规范用八进制书写，把八进制数字当二进制读即可）：
    ///   L=00→0  B=10→8  BOOLEAN=11→9  A=20→16  JIS8=21→17
    ///   I8=30→24 I1=31→25 I2=32→26 I4=34→28
    ///   F8=40→32 F4=44→36
    ///   U8=50→40 U1=51→41 U2=52→42 U4=54→44
    ///
    /// 数据项首字节 = (格式码 &lt;&lt; 2) | 长度字节数，所以
    ///   A + 1 字节长度 → 0x41、L + 1 字节长度 → 0x01、U1 + 1 字节长度 → 0xA5。
    /// </summary>
    public enum SecsFormat
    {
        /// <summary>列表（长度字段 = **子项个数**，不是字节数）。</summary>
        List = 0,
        /// <summary>二进制（每元素 1 字节）。</summary>
        Binary = 8,
        /// <summary>布尔（每元素 1 字节，0/1）。</summary>
        Boolean = 9,
        /// <summary>ASCII 字符串（长度 = 字节数）。</summary>
        Ascii = 16,
        /// <summary>JIS-8 字符串（长度 = 字节数）。</summary>
        Jis8 = 17,
        /// <summary>8 字节有符号整数。</summary>
        I8 = 24,
        /// <summary>1 字节有符号整数。</summary>
        I1 = 25,
        /// <summary>2 字节有符号整数。</summary>
        I2 = 26,
        /// <summary>4 字节有符号整数。</summary>
        I4 = 28,
        /// <summary>8 字节 IEEE754 浮点。</summary>
        F8 = 32,
        /// <summary>4 字节 IEEE754 浮点。</summary>
        F4 = 36,
        /// <summary>8 字节无符号整数。</summary>
        U8 = 40,
        /// <summary>1 字节无符号整数。</summary>
        U1 = 41,
        /// <summary>2 字节无符号整数。</summary>
        U2 = 42,
        /// <summary>4 字节无符号整数。</summary>
        U4 = 44,
    }

    /// <summary>
    /// 一个 SECS-II 数据项：格式 + 值。
    ///
    /// 三种承载方式（按格式区分，互不混用）：
    ///   - <see cref="Children"/>：仅 <see cref="SecsFormat.List"/> 用，长度字段写「项数」
    ///   - <see cref="Raw"/>：A / JIS8 的原始字节（长度字段写字节数）
    ///   - <see cref="Numbers"/> / <see cref="Floats"/>：B / BOOLEAN / I* / U* 用 Numbers，F4 / F8 用 Floats
    ///
    /// 文本侧提供 SML（SECS Message Language，半导体行业描述 SECS-II 的通用写法）：
    ///   <c>&lt;L &lt;A "START"&gt; &lt;U4 1 2 3&gt; &gt;</c>、<c>&lt;B 0x01 0x02&gt;</c>、
    ///   <c>&lt;BOOLEAN TRUE FALSE&gt;</c>、<c>&lt;F8 1.5&gt;</c>、空项 <c>&lt;&gt;</c>。
    ///   二进制与文本都能往返（冒烟里逐格式对拍）。
    /// </summary>
    public sealed class SecsItem
    {
        /// <summary>数据项格式。</summary>
        public SecsFormat Format { get; }

        /// <summary>列表子项（仅 L）。</summary>
        public List<SecsItem> Children { get; } = new List<SecsItem>();

        /// <summary>整数类元素（B / BOOLEAN / I* / U*）。B 的元素范围 0~255。</summary>
        public List<long> Ints { get; } = new List<long>();

        /// <summary>浮点类元素（F4 / F8）。</summary>
        public List<double> Reals { get; } = new List<double>();

        /// <summary>A / JIS8 的原始字节。</summary>
        public byte[] Raw { get; set; }

        /// <summary>元素个数：L 为子项数，A/JIS8 为字节数，其余为元素数。</summary>
        public int Count
        {
            get
            {
                switch (Format)
                {
                    case SecsFormat.List: return Children.Count;
                    case SecsFormat.Ascii:
                    case SecsFormat.Jis8: return Raw == null ? 0 : Raw.Length;
                    case SecsFormat.F4:
                    case SecsFormat.F8: return Reals.Count;
                    default: return Ints.Count;
                }
            }
        }

        /// <summary>构造一个空的数据项（解析器与工厂方法用）。</summary>
        public SecsItem(SecsFormat format) => Format = format;

        // ---------------------------------------------------------------- 工厂

        /// <summary>列表项。</summary>
        public static SecsItem L(params SecsItem[] children)
        {
            var it = new SecsItem(SecsFormat.List);
            if (children != null) it.Children.AddRange(children);
            return it;
        }

        /// <summary>ASCII 字符串项（按 UTF-8 编码；ASCII 文本字节与 UTF-8 一致）。</summary>
        public static SecsItem A(string text) =>
            new SecsItem(SecsFormat.Ascii) { Raw = Encoding.UTF8.GetBytes(text ?? string.Empty) };

        /// <summary>JIS-8 字符串项。</summary>
        public static SecsItem Jis(string text) =>
            new SecsItem(SecsFormat.Jis8) { Raw = Encoding.UTF8.GetBytes(text ?? string.Empty) };

        /// <summary>二进制项（每个值 0~255）。</summary>
        public static SecsItem B(params int[] bytes)
        {
            var it = new SecsItem(SecsFormat.Binary);
            if (bytes != null)
                foreach (var b in bytes) it.Ints.Add(b & 0xFF);
            return it;
        }

        /// <summary>布尔项。</summary>
        public static SecsItem Bool(params bool[] values)
        {
            var it = new SecsItem(SecsFormat.Boolean);
            if (values != null)
                foreach (var v in values) it.Ints.Add(v ? 1 : 0);
            return it;
        }

        /// <summary>整数项（格式必须是 B / BOOLEAN / I* / U*）。★ 不能叫 Ints：与同名实例属性冲突（CS0102）。</summary>
        public static SecsItem Numbers(SecsFormat format, params long[] values)
        {
            var it = new SecsItem(format);
            if (values != null) it.Ints.AddRange(values);
            return it;
        }

        /// <summary>浮点项（格式必须是 F4 / F8）。★ 不能叫 Reals：与同名实例属性冲突（CS0102）。</summary>
        public static SecsItem Floats(SecsFormat format, params double[] values)
        {
            var it = new SecsItem(format);
            if (values != null) it.Reals.AddRange(values);
            return it;
        }

        public static SecsItem U1(params long[] v) => Numbers(SecsFormat.U1, v);
        public static SecsItem U2(params long[] v) => Numbers(SecsFormat.U2, v);
        public static SecsItem U4(params long[] v) => Numbers(SecsFormat.U4, v);
        public static SecsItem U8(params long[] v) => Numbers(SecsFormat.U8, v);
        public static SecsItem I1(params long[] v) => Numbers(SecsFormat.I1, v);
        public static SecsItem I2(params long[] v) => Numbers(SecsFormat.I2, v);
        public static SecsItem I4(params long[] v) => Numbers(SecsFormat.I4, v);
        public static SecsItem I8(params long[] v) => Numbers(SecsFormat.I8, v);
        public static SecsItem F4(params double[] v) => Floats(SecsFormat.F4, v);
        public static SecsItem F8(params double[] v) => Floats(SecsFormat.F8, v);

        // ---------------------------------------------------------------- 名称

        /// <summary>格式名（SML 里用的写法）。</summary>
        public static string FormatName(SecsFormat f)
        {
            switch (f)
            {
                case SecsFormat.List: return "L";
                case SecsFormat.Binary: return "B";
                case SecsFormat.Boolean: return "BOOLEAN";
                case SecsFormat.Ascii: return "A";
                case SecsFormat.Jis8: return "JIS8";
                case SecsFormat.I8: return "I8";
                case SecsFormat.I1: return "I1";
                case SecsFormat.I2: return "I2";
                case SecsFormat.I4: return "I4";
                case SecsFormat.F8: return "F8";
                case SecsFormat.F4: return "F4";
                case SecsFormat.U8: return "U8";
                case SecsFormat.U1: return "U1";
                case SecsFormat.U2: return "U2";
                case SecsFormat.U4: return "U4";
                default: return "?";
            }
        }

        /// <summary>格式名 → 格式码（大小写不敏感）。</summary>
        public static bool TryParseFormat(string name, out SecsFormat format)
        {
            format = SecsFormat.List;
            if (string.IsNullOrWhiteSpace(name)) return false;
            switch (name.Trim().ToUpperInvariant())
            {
                case "L": format = SecsFormat.List; return true;
                case "B": case "BIN": case "BINARY": format = SecsFormat.Binary; return true;
                case "BOOLEAN": case "BOOL": case "TF": format = SecsFormat.Boolean; return true;
                case "A": case "ASCII": format = SecsFormat.Ascii; return true;
                case "JIS8": case "JIS": format = SecsFormat.Jis8; return true;
                case "I8": format = SecsFormat.I8; return true;
                case "I1": format = SecsFormat.I1; return true;
                case "I2": format = SecsFormat.I2; return true;
                case "I4": format = SecsFormat.I4; return true;
                case "F8": format = SecsFormat.F8; return true;
                case "F4": format = SecsFormat.F4; return true;
                case "U8": format = SecsFormat.U8; return true;
                case "U1": format = SecsFormat.U1; return true;
                case "U2": format = SecsFormat.U2; return true;
                case "U4": format = SecsFormat.U4; return true;
                default: return false;
            }
        }

        /// <summary>是否已知的 6 位格式码。</summary>
        public static bool IsKnownFormatCode(int code)
        {
            switch ((SecsFormat)code)
            {
                case SecsFormat.List:
                case SecsFormat.Binary:
                case SecsFormat.Boolean:
                case SecsFormat.Ascii:
                case SecsFormat.Jis8:
                case SecsFormat.I8:
                case SecsFormat.I1:
                case SecsFormat.I2:
                case SecsFormat.I4:
                case SecsFormat.F8:
                case SecsFormat.F4:
                case SecsFormat.U8:
                case SecsFormat.U1:
                case SecsFormat.U2:
                case SecsFormat.U4:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>单个元素占几个字节（L / A / JIS8 返回 1，A 的长度按字节算）。</summary>
        public static int ElementSize(SecsFormat f)
        {
            switch (f)
            {
                case SecsFormat.Binary:
                case SecsFormat.Boolean:
                case SecsFormat.I1:
                case SecsFormat.U1:
                    return 1;
                case SecsFormat.I2:
                case SecsFormat.U2:
                    return 2;
                case SecsFormat.I4:
                case SecsFormat.U4:
                case SecsFormat.F4:
                    return 4;
                case SecsFormat.I8:
                case SecsFormat.U8:
                case SecsFormat.F8:
                    return 8;
                default:
                    return 1;
            }
        }

        // ---------------------------------------------------------------- 编码

        /// <summary>把本项（含子项）追加编码到缓冲区。</summary>
        public void Encode(List<byte> outp)
        {
            if (outp == null) throw new ArgumentNullException(nameof(outp));

            byte[] body;
            int count;   // ★ List 的长度字段是「项数」，其余格式是「字节数」

            if (Format == SecsFormat.List)
            {
                var inner = new List<byte>();
                foreach (var c in Children) c.Encode(inner);
                body = inner.ToArray();
                count = Children.Count;
            }
            else if (Format == SecsFormat.Ascii || Format == SecsFormat.Jis8)
            {
                body = Raw ?? Array.Empty<byte>();
                count = body.Length;
            }
            else if (Format == SecsFormat.F4 || Format == SecsFormat.F8)
            {
                body = EncodeReals();
                count = Reals.Count;
            }
            else
            {
                body = EncodeInts();
                count = Ints.Count;
            }

            int lenBytes = count <= 0xFF ? 1 : (count <= 0xFFFF ? 2 : 3);
            outp.Add((byte)(((int)Format << 2) | lenBytes));
            for (int i = lenBytes - 1; i >= 0; i--)
                outp.Add((byte)((count >> (8 * i)) & 0xFF));
            outp.AddRange(body);
        }

        /// <summary>编码为独立字节数组。</summary>
        public byte[] ToBytes()
        {
            var buf = new List<byte>();
            Encode(buf);
            return buf.ToArray();
        }

        private byte[] EncodeInts()
        {
            int size = ElementSize(Format);
            var body = new byte[Ints.Count * size];
            for (int i = 0; i < Ints.Count; i++)
            {
                ulong raw = unchecked((ulong)Ints[i]);
                for (int b = 0; b < size; b++)
                    body[i * size + b] = (byte)((raw >> (8 * (size - 1 - b))) & 0xFF);
            }
            return body;
        }

        private byte[] EncodeReals()
        {
            if (Format == SecsFormat.F4)
            {
                var body = new byte[Reals.Count * 4];
                for (int i = 0; i < Reals.Count; i++)
                {
                    uint bits = unchecked((uint)BitConverter.SingleToInt32Bits((float)Reals[i]));
                    for (int b = 0; b < 4; b++)
                        body[i * 4 + b] = (byte)((bits >> (8 * (3 - b))) & 0xFF);
                }
                return body;
            }
            else
            {
                var body = new byte[Reals.Count * 8];
                for (int i = 0; i < Reals.Count; i++)
                {
                    ulong bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(Reals[i]));
                    for (int b = 0; b < 8; b++)
                        body[i * 8 + b] = (byte)((bits >> (8 * (7 - b))) & 0xFF);
                }
                return body;
            }
        }

        // ---------------------------------------------------------------- 解码

        /// <summary>
        /// 从 <paramref name="buf"/> 的 <paramref name="pos"/> 处解码一个数据项（递归解列表）。
        /// 成功返回数据项并把 pos 推到该项之后；失败返回 null 并给出中文原因。
        /// </summary>
        public static SecsItem Decode(byte[] buf, ref int pos, out string error)
        {
            error = null;
            if (buf == null) { error = "缓冲区为空"; return null; }
            if (pos < 0 || pos >= buf.Length) { error = "数据项越界：已到报文末尾"; return null; }

            int first = buf[pos++];
            int code = (first >> 2) & 0x3F;
            int lenBytes = first & 0x03;

            if (lenBytes == 0)
            {
                error = "非法长度字节数 0（数据项首字节 0x" + first.ToString("X2", CultureInfo.InvariantCulture) + "）";
                return null;
            }
            if (!IsKnownFormatCode(code))
            {
                error = "未知 SECS-II 格式码 " + code.ToString(CultureInfo.InvariantCulture)
                        + "（数据项首字节 0x" + first.ToString("X2", CultureInfo.InvariantCulture) + "）";
                return null;
            }
            if (pos + lenBytes > buf.Length)
            {
                error = "长度字段越界（需要 " + lenBytes.ToString(CultureInfo.InvariantCulture) + " 字节）";
                return null;
            }

            int count = 0;
            for (int i = 0; i < lenBytes; i++)
                count = (count << 8) | buf[pos++];

            var fmt = (SecsFormat)code;
            var item = new SecsItem(fmt);

            if (fmt == SecsFormat.List)
            {
                for (int i = 0; i < count; i++)
                {
                    var child = Decode(buf, ref pos, out error);
                    if (child == null)
                    {
                        error = "列表第 " + (i + 1).ToString(CultureInfo.InvariantCulture) + " 项解码失败：" + error;
                        return null;
                    }
                    item.Children.Add(child);
                }
                return item;
            }

            if (fmt == SecsFormat.Ascii || fmt == SecsFormat.Jis8)
            {
                if (pos + count > buf.Length)
                {
                    error = FormatName(fmt) + " 项长度 " + count.ToString(CultureInfo.InvariantCulture) + " 超出剩余字节";
                    return null;
                }
                item.Raw = new byte[count];
                Array.Copy(buf, pos, item.Raw, 0, count);
                pos += count;
                return item;
            }

            int size = ElementSize(fmt);
            long need = (long)count * size;
            if (pos + need > buf.Length)
            {
                error = FormatName(fmt) + " 项需要 " + need.ToString(CultureInfo.InvariantCulture)
                        + " 字节，超出剩余 " + (buf.Length - pos).ToString(CultureInfo.InvariantCulture) + " 字节";
                return null;
            }

            for (int i = 0; i < count; i++)
            {
                ulong raw = 0;
                for (int b = 0; b < size; b++)
                    raw = (raw << 8) | buf[pos++];

                if (fmt == SecsFormat.F4)
                {
                    item.Reals.Add(BitConverter.Int32BitsToSingle(unchecked((int)(uint)raw)));
                }
                else if (fmt == SecsFormat.F8)
                {
                    item.Reals.Add(BitConverter.Int64BitsToDouble(unchecked((long)raw)));
                }
                else if (fmt == SecsFormat.I1 || fmt == SecsFormat.I2 || fmt == SecsFormat.I4 || fmt == SecsFormat.I8)
                {
                    // 符号扩展：把 size 字节按补码还原
                    int shift = 64 - size * 8;
                    item.Ints.Add(unchecked((long)raw << shift) >> shift);
                }
                else
                {
                    item.Ints.Add(unchecked((long)raw));
                }
            }
            return item;
        }

        // ---------------------------------------------------------------- SML 文本

        /// <summary>多行缩进的 SML 文本（便于人读 / 复制）。</summary>
        public string ToSml() => ToSmlCore(0, true);

        /// <summary>单行紧凑的 SML 文本（日志里一行一条）。</summary>
        public string ToSmlLine() => ToSmlCore(0, false);

        private string ToSmlCore(int indent, bool multi)
        {
            string pad = multi ? new string(' ', indent) : string.Empty;
            string padIn = multi ? new string(' ', indent + 2) : string.Empty;

            switch (Format)
            {
                case SecsFormat.List:
                    {
                        // ★ 必须自己一对花括号：不然这里的 sb 会落进 switch 作用域，
                        //   和下面各 case 花括号里的 sb 撞名（CS0136）。
                        if (Children.Count == 0) return "<L>";
                        if (!multi)
                        {
                            var sb0 = new StringBuilder("<L");
                            foreach (var c in Children) sb0.Append(' ').Append(c.ToSmlCore(indent, false));
                            return sb0.Append('>').ToString();
                        }
                        var sb = new StringBuilder("<L");
                        foreach (var c in Children)
                            sb.Append('\n').Append(padIn).Append(c.ToSmlCore(indent + 2, true));
                        return sb.Append('\n').Append(pad).Append('>').ToString();
                    }

                case SecsFormat.Ascii:
                case SecsFormat.Jis8:
                    return "<" + FormatName(Format) + " \"" + EscapeText(Raw) + "\">";

                case SecsFormat.Binary:
                    {
                        var sb = new StringBuilder("<B");
                        foreach (var v in Ints)
                            sb.Append(" 0x").Append(((byte)v).ToString("X2", CultureInfo.InvariantCulture));
                        return sb.Append('>').ToString();
                    }

                case SecsFormat.Boolean:
                    {
                        var sb = new StringBuilder("<BOOLEAN");
                        foreach (var v in Ints) sb.Append(v != 0 ? " TRUE" : " FALSE");
                        return sb.Append('>').ToString();
                    }

                case SecsFormat.F4:
                case SecsFormat.F8:
                    {
                        var sb = new StringBuilder("<" + FormatName(Format));
                        foreach (var v in Reals)
                            sb.Append(' ').Append(v.ToString("R", CultureInfo.InvariantCulture));
                        return sb.Append('>').ToString();
                    }

                default:
                    {
                        var sb = new StringBuilder("<" + FormatName(Format));
                        foreach (var v in Ints)
                            sb.Append(' ').Append(v.ToString(CultureInfo.InvariantCulture));
                        return sb.Append('>').ToString();
                    }
            }
        }

        private static string EscapeText(byte[] raw)
        {
            if (raw == null || raw.Length == 0) return string.Empty;
            var sb = new StringBuilder(raw.Length);
            foreach (var b in raw)
            {
                switch (b)
                {
                    case (byte)'"': sb.Append("\\\""); break;
                    case (byte)'\\': sb.Append("\\\\"); break;
                    case (byte)'\r': sb.Append("\\r"); break;
                    case (byte)'\n': sb.Append("\\n"); break;
                    case (byte)'\t': sb.Append("\\t"); break;
                    default:
                        if (b >= 0x20 && b < 0x7F) sb.Append((char)b);
                        else sb.Append("\\x").Append(b.ToString("X2", CultureInfo.InvariantCulture));
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 解析 SML 文本里的**一个**数据项，并把 <paramref name="pos"/> 推到该项之后。
        /// 失败返回 null 并给出中文原因。空白随意，支持多行。
        /// </summary>
        public static SecsItem ParseSml(string text, ref int pos, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(text)) { error = "数据项文本为空"; return null; }

            SkipWs(text, ref pos);
            if (pos >= text.Length) { error = "数据项文本意外结束（缺少 '<'）"; return null; }
            if (text[pos] != '<') { error = "第 " + (pos + 1).ToString(CultureInfo.InvariantCulture) + " 个字符应为 '<'，实际是 '" + text[pos] + "'"; return null; }
            pos++;

            SkipWs(text, ref pos);
            if (pos < text.Length && text[pos] == '>')
            {
                pos++;
                return L();   // <> 约定为空列表
            }

            int nameStart = pos;
            while (pos < text.Length && (char.IsLetterOrDigit(text[pos]))) pos++;
            string name = text.Substring(nameStart, pos - nameStart);
            if (name.Length == 0)
            {
                error = "缺少数据项格式名（如 L / A / U4 / BOOLEAN）";
                return null;
            }
            if (!TryParseFormat(name, out SecsFormat fmt))
            {
                error = "未知数据项格式「" + name + "」";
                return null;
            }

            if (fmt == SecsFormat.List)
            {
                var list = L();
                while (true)
                {
                    SkipWs(text, ref pos);
                    if (pos >= text.Length) { error = "列表缺少闭合的 '>'"; return null; }
                    if (text[pos] == '>') { pos++; return list; }
                    var child = ParseSml(text, ref pos, out error);
                    if (child == null) return null;
                    list.Children.Add(child);
                }
            }

            var item = new SecsItem(fmt);

            if (fmt == SecsFormat.Ascii || fmt == SecsFormat.Jis8)
            {
                SkipWs(text, ref pos);
                if (pos < text.Length && text[pos] == '"')
                {
                    pos++;
                    var raw = new List<byte>();
                    bool closed = false;
                    while (pos < text.Length)
                    {
                        char ch = text[pos];
                        if (ch == '\\' && pos + 1 < text.Length)
                        {
                            pos++;
                            raw.AddRange(DecodeEscape(text, ref pos));
                            continue;
                        }
                        if (ch == '"') { pos++; closed = true; break; }
                        raw.AddRange(Encoding.UTF8.GetBytes(ch.ToString()));
                        pos++;
                    }
                    if (!closed) { error = "字符串缺少闭合的 '\"'"; return null; }
                    item.Raw = raw.ToArray();
                }
                else
                {
                    int start = pos;
                    while (pos < text.Length && text[pos] != '>' && !char.IsWhiteSpace(text[pos])) pos++;
                    if (pos == start) { error = FormatName(fmt) + " 项缺少字符串内容"; return null; }
                    item.Raw = Encoding.UTF8.GetBytes(text.Substring(start, pos - start));
                }
                SkipWs(text, ref pos);
                if (pos >= text.Length || text[pos] != '>')
                {
                    error = FormatName(fmt) + " 项缺少闭合的 '>'";
                    return null;
                }
                pos++;
                return item;
            }

            // 数值 / 布尔 / 二进制：读到 '>' 为止，token 以空白分隔
            while (true)
            {
                SkipWs(text, ref pos);
                if (pos >= text.Length) { error = FormatName(fmt) + " 项缺少闭合的 '>'"; return null; }
                if (text[pos] == '>') { pos++; return item; }

                int start = pos;
                while (pos < text.Length && text[pos] != '>' && !char.IsWhiteSpace(text[pos])) pos++;
                string tok = text.Substring(start, pos - start);
                if (tok.Length == 0) { error = FormatName(fmt) + " 项里有空 token"; return null; }

                if (!AddScalarToken(item, tok, out error)) return null;
            }
        }

        private static bool AddScalarToken(SecsItem item, string tok, out string error)
        {
            error = null;
            switch (item.Format)
            {
                case SecsFormat.Boolean:
                    {
                        var up = tok.ToUpperInvariant();
                        if (up == "TRUE" || up == "T" || up == "1") { item.Ints.Add(1); return true; }
                        if (up == "FALSE" || up == "F" || up == "0") { item.Ints.Add(0); return true; }
                        if (TryParseLong(tok, out long bv) && (bv == 0 || bv == 1)) { item.Ints.Add(bv); return true; }
                        error = "BOOLEAN 项里的值「" + tok + "」不是 TRUE/FALSE/1/0";
                        return false;
                    }
                case SecsFormat.Binary:
                    {
                        if (!TryParseLong(tok, out long bv) || bv < 0 || bv > 255)
                        {
                            error = "B 项里的值「" + tok + "」不是 0~255 的字节";
                            return false;
                        }
                        item.Ints.Add(bv);
                        return true;
                    }
                case SecsFormat.F4:
                case SecsFormat.F8:
                    {
                        if (!double.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv))
                        {
                            error = FormatName(item.Format) + " 项里的值「" + tok + "」不是合法浮点数";
                            return false;
                        }
                        item.Reals.Add(dv);
                        return true;
                    }
                default:
                    {
                        if (!TryParseLong(tok, out long iv))
                        {
                            error = FormatName(item.Format) + " 项里的值「" + tok + "」不是合法整数";
                            return false;
                        }
                        item.Ints.Add(iv);
                        return true;
                    }
            }
        }

        private static bool TryParseLong(string tok, out long value)
        {
            value = 0;
            if (string.IsNullOrEmpty(tok)) return false;
            if (tok.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                || tok.StartsWith("-0x", StringComparison.OrdinalIgnoreCase))
            {
                bool neg = tok[0] == '-';
                string hex = tok.Substring(neg ? 3 : 2);
                if (hex.Length == 0 || !ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong uv))
                    return false;
                value = neg ? unchecked(-(long)uv) : unchecked((long)uv);
                return true;
            }
            return long.TryParse(tok, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static byte[] DecodeEscape(string text, ref int pos)
        {
            // pos 指向反斜杠之后的那个字符
            char c = text[pos++];
            switch (c)
            {
                case 'r': return new[] { (byte)'\r' };
                case 'n': return new[] { (byte)'\n' };
                case 't': return new[] { (byte)'\t' };
                case '0': return new[] { (byte)0 };
                case '\\': return new[] { (byte)'\\' };
                case '"': return new[] { (byte)'"' };
                case 'x':
                case 'X':
                    if (pos + 1 < text.Length
                        && byte.TryParse(text.Substring(pos, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hv))
                    {
                        pos += 2;
                        return new[] { hv };
                    }
                    return new[] { (byte)c };
                default:
                    return Encoding.UTF8.GetBytes(c.ToString());
            }
        }

        private static void SkipWs(string text, ref int pos)
        {
            while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
        }

        /// <summary>一行摘要（日志用）。</summary>
        public string Describe()
        {
            if (Format == SecsFormat.List)
                return "L[" + Children.Count.ToString(CultureInfo.InvariantCulture) + "]";
            if (Format == SecsFormat.Ascii || Format == SecsFormat.Jis8)
                return FormatName(Format) + "[" + Count.ToString(CultureInfo.InvariantCulture) + "]";
            return FormatName(Format) + "[" + Count.ToString(CultureInfo.InvariantCulture) + "]";
        }

        /// <summary>调试用：整棵树一行 SML。</summary>
        public override string ToString() => ToSmlLine();
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
