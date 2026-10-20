// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
#nullable disable
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Hardware.Comm
{
    /// <summary>
    /// Web(HTTP/HTTPS) 通道（CommType = Web）：和 MES / WebAPI / 第三方平台用 HTTP 交换数据。
    /// <para>「地址」填完整 URL；「请求方式」决定发送怎么发：<c>POST</c>（默认，带正文）、
    /// <c>PUT</c>（带正文）、<c>GET</c>（无正文，正文非空时按 <c>?data=…</c> 追加到 URL）。</para>
    /// <para>接收 = 对该 URL 发一次 GET，把响应正文交给脚本（纯文本原样返回；含不可见字节时转成
    /// <c>HEX:..</c>，与串口 / 网口通道同一套编码约定）。</para>
    /// <para>★ 全进程共用一个 <see cref="HttpClient"/>：每次收发 new 一个 client 会把端口耗光
    /// （TIME_WAIT 堆积），现场表现为跑一会儿就开始随机超时。</para>
    /// <para>超时用「超时」字段（含连接 + 读响应）；失败一律抛中文 <see cref="IOException"/>
    /// 说清楚是地址不通、还是对方返回了 4xx/5xx。</para>
    /// </summary>
    public sealed class HttpCommChannel : ICommChannel
    {
        // 共享一个实例：HttpClient 设计上就是可复用、可并发的，自己管连接池
        private static readonly HttpClient Http = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = false,
            // ★ 必须绕开系统代理：本机的代理设置会把发往 127.0.0.1 / 局域网设备的请求转给代理，
            //   代理不认识就一路挂到超时（现场表现为「浏览器能开、软件里超时」）。
            //   本软件是设备对接工具，直连才是预期行为；真要过代理请在 URL 那侧自己配。
            UseProxy = false,
        });

        private readonly CommItem _cfg;

        public HttpCommChannel(CommItem cfg) => _cfg = cfg;

        public string Name => _cfg.Name;

        /// <summary>最近一次请求是否成功（HTTP 无长连接，「已连接」= 上一次请求通了）。</summary>
        public bool IsOpen { get; private set; }

        /// <summary>最近一次失败原因，供通讯页显示。</summary>
        public string LastError { get; private set; } = string.Empty;

        private string Url
        {
            get
            {
                string u = (_cfg.PortOrIp ?? string.Empty).Trim().Trim('"');
                if (u.Length == 0)
                    throw new IOException("Web 地址为空：请在通讯配置里填写完整 URL（如 http://127.0.0.1:8080/api/io）。");
                if (!u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    && !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    u = "http://" + u;
                if (!Uri.TryCreate(u, UriKind.Absolute, out _))
                    throw new IOException($"Web 地址不是合法 URL：{u}");
                return u;
            }
        }

        private string Method => (_cfg.WebMethod ?? "POST").Trim().ToUpperInvariant();

        private string ContentType => string.IsNullOrWhiteSpace(_cfg.WebContentType) ? "text/plain" : _cfg.WebContentType.Trim();

        public void Open()
        {
            string _ = Url;            // 只做地址合法性检查，HTTP 不需要保持连接
        }

        public void Send(string data) => SendAndReply(data);

        /// <summary>发送并取回响应正文（通讯页与 Lua 都能看到对方回了什么）。</summary>
        public string SendAndReply(string data)
        {
            string url = Url;
            string m = Method;
            byte[] body = PayloadCodec.Encode(data);

            using (var req = new HttpRequestMessage(new HttpMethod(m), url))
            {
                if (m == "GET" && body.Length > 0)
                {
                    string sep = url.IndexOf('?') >= 0 ? "&" : "?";
                    req.RequestUri = new Uri(url + sep + "data=" + Uri.EscapeDataString(Encoding.UTF8.GetString(body)));
                }
                else if (m != "GET" && body.Length > 0)
                {
                    var content = new ByteArrayContent(body);
                    content.Headers.TryAddWithoutValidation("Content-Type", ContentType);
                    req.Content = content;
                }
                return Execute(req);
            }
        }

        public string Recv()
        {
            using (var req = new HttpRequestMessage(HttpMethod.Get, Url))
                return Execute(req);
        }

        private string Execute(HttpRequestMessage req)
        {
            int timeout = _cfg.TimeoutMs > 0 ? _cfg.TimeoutMs : 5000;
            using (var cts = new CancellationTokenSource(timeout))
            {
                try
                {
                    using (var resp = Http.SendAsync(req, cts.Token).GetAwaiter().GetResult())
                    {
                        byte[] bytes = resp.Content != null
                            ? resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
                            : Array.Empty<byte>();

                        if (!resp.IsSuccessStatusCode)
                        {
                            IsOpen = false;
                            string text = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : string.Empty;
                            if (text.Length > 200) text = text.Substring(0, 200) + "…";
                            LastError = $"对方返回 {(int)resp.StatusCode} {resp.ReasonPhrase}：{text}";
                            throw new IOException($"HTTP 请求失败：{req.RequestUri} → {(int)resp.StatusCode} {resp.ReasonPhrase}"
                                                  + (text.Length == 0 ? "" : $"（{text}）"));
                        }

                        IsOpen = true;
                        LastError = string.Empty;
                        return PayloadCodec.Decode(bytes, bytes.Length);
                    }
                }
                catch (IOException) { throw; }
                catch (OperationCanceledException)
                {
                    IsOpen = false;
                    LastError = $"请求 {req.RequestUri} 超时（{timeout}ms）";
                    throw new IOException($"请求 {req.RequestUri} 超时（{timeout}ms）：请检查对方服务是否开着、地址与端口是否正确。");
                }
                catch (HttpRequestException ex)
                {
                    IsOpen = false;
                    LastError = $"请求 {req.RequestUri} 失败：{ex.Message}";
                    throw new IOException($"请求 {req.RequestUri} 失败：{ex.Message}"
                                          + "（连不上通常是地址/端口错、对方服务未启动、被防火墙或代理拦了）");
                }
            }
        }

        public void Dispose() { /* 共享 HttpClient 不随通道释放 */ }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
