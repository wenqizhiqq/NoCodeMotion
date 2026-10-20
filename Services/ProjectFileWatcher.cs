// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.IO;
using System.Windows.Threading;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 工程文件「外部改动自动载入」：盯住当前工程那个 <c>Projects\工程名.xlsx</c>，
    /// 被外部（AI / agent / Excel / 脚本）改写后弹窗问一句 —— 载入文件 / 用软件里的配置还原文件 / 稍后。
    /// 配套「项目管理」页的【复制路径与说明】：复制路径 + 操作约定 → 交给 agent 直接改这个文件 → 这边识别变化再问。
    /// <para>★ 为什么是弹窗而不是静默重载：这个 xlsx 就是工程的唯一存储，静默把内存里的配置换掉风险太高
    /// （用户可能正在软件里改到一半），而「还原文件」这条退路只有弹窗里给才来得及按。</para>
    /// </summary>
    /// <remarks>
    /// 三个坑不处理，这个功能比不加还糟：
    /// ① **软件自己写的盘不算外部改动**。任何一处配置改动都会走 ProjectStore 防抖 400ms 写盘，
    ///    不屏蔽就成了「保存 → 被判成外部改动 → 重载」的自循环，重载还会把正在输入的焦点与滚动位置冲掉。
    ///    写盘收口只有一处（ProjectManager.WriteFile），所以由它回调 <see cref="NoteSelfWrite"/> 记时间戳。
    /// ② **「有变化」不等于「写完了」**。外部工具写 xlsx 会占着文件、期间连着刷新时间戳，读早了拿到半截文件。
    ///    所以只做时间戳轮询：连续两轮读到同一个新时间才动手；真去读时若还被占着，本轮跳过、下一轮再试。
    /// ③ **解析失败绝不能清空当前工程**。<c>OpenProject</c> 读失败会退化成空工程，
    ///    自动重载若走那条路，agent 一次写坏文件就把用户全部配置抹了。所以走
    ///    <see cref="ProjectManager.ReloadCurrentFromDiskAsync"/>：先解析到临时对象，成功才原地替换。
    /// </remarks>
    public static class ProjectFileWatcher
    {
        /// <summary>轮询间隔（毫秒）。要连续两轮同一个新的时间戳才动手，所以识别延迟约 2×该值。</summary>
        private const int PollIntervalMs = 800;

        private static DispatcherTimer? _timer;
        private static string _trackedName = "";      // 正在盯的工程名（= 当时的当前工程）
        private static DateTime _known;               // 我们认得的该文件最后写入时间（UTC）
        private static DateTime _pending;             // 上一轮读到的「比 _known 新」的时间
        private static bool _busy;                    // 重载是异步的，防重入

        /// <summary>开始轮询。主窗口启动流程收尾处调用一次即可（重复调用无副作用）。</summary>
        public static void Start()
        {
            if (_timer != null) return;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(PollIntervalMs) };
            _timer.Tick += async (_, __) => await TickAsync();
            _timer.Start();
        }

        /// <summary>
        /// 软件自己写了某个工程的文件：把新的时间戳记成「已知」，别让自动载入把自己的保存误判成外部改动。
        /// </summary>
        public static void NoteSelfWrite(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            var t = LastWriteUtc(name);
            if (name != _trackedName) _trackedName = name;
            _known = t;
            _pending = default;
        }

        /// <summary>
        /// 现在有没有「写者」占着这个工程文件。用 <c>FileAccess.Read + FileShare.Read</c> 探一下：
        /// 我自己只要读权限，同时只允许别人读 —— 有人正握着写权限（软件写盘 / agent 保存 / Excel 改完没落盘）
        /// 就打不开，返回 false。只有读者（比如 Excel 打开着看）不会挡住重载。
        /// </summary>
        private static bool HasNoWriter(string name)
        {
            try
            {
                using (new FileStream(ProjectManager.FilePathOf(name),
                                      FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private static DateTime LastWriteUtc(string name)
        {
            try
            {
                var path = ProjectManager.FilePathOf(name);
                return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : default;
            }
            catch { return default; }     // 文件正被外部工具独占时，元信息也可能读不动 → 当作「本轮没看到变化」
        }

        private static async System.Threading.Tasks.Task TickAsync()
        {
            if (_busy) return;
            var name = ProjectManager.CurrentName;
            if (string.IsNullOrEmpty(name)) return;

            // 换了工程（打开 / 新建 / 重命名）：以新文件的时间戳重新起算，别把切换当成外部改动。
            if (name != _trackedName)
            {
                _trackedName = name;
                _known = LastWriteUtc(name);
                _pending = default;
                return;
            }

            var t = LastWriteUtc(name);
            if (t == default)
            {
                // 文件不见了（被删 / 被改名 / 一直被占着）：清掉基准，等它回来再重新起算。
                _known = default;
                _pending = default;
                return;
            }
            if (t <= _known) { _pending = default; return; }      // 没有新改动

            if (_pending != t) { _pending = t; return; }          // 第一轮：先记下，等下一轮确认写稳

            // 时间戳稳了还不够：文件可能正被写者占着（外部工具还没关，或软件自己在写一个大工程）。
            // 这时保持 _pending 不动，下一轮再试 —— 读半截文件会误报「工程读不了」。
            if (!HasNoWriter(name)) return;
            _pending = default;

            // 流程正在跑：换数据等于把轴 / IO 定义从运行中的脚本脚下抽走，这一轮先不动，下一轮再判。
            if (StatusBarService.IsRunning) return;

            // ★ 先把文件读出来、算清「改了什么」，再弹窗给用户看 —— 看不见改动内容就让人决定载不载入，等于让人猜。
            //   模态框会跑嵌套消息泵，DispatcherTimer 在框开着时照样会 Tick，所以 _busy 必须在弹框之前就置位。
            _busy = true;
            try
            {
                var (disk, err) = await ProjectManager.ReadProjectFromDiskAsync(name);
                if (disk == null)
                {
                    // 读不动（AI 写了一半 / 格式坏了 / 还被占着）：不弹「载入 / 还原」的框 —— 载入必然失败，
                    // 点了只会更困惑。只在状态栏报一次，并认下时间戳，免得同一份坏文件反复报。
                    _known = LastWriteUtc(name);
                    if (err != null) StatusBarService.ReportException(err);
                    return;
                }

                var diff = ProjectChangeSummary.Build(disk, ProjectStore.Data);
                var dlg = new Views.ConfirmDialog(
                    "工程文件被外部修改",
                    $"工程「{name}」的 xlsx 在软件外面被改过了（{t.ToLocalTime():HH:mm:ss}）。\n\n"
                    + "■ 改了什么\n" + diff + "\n\n"
                    + "■ 怎么处理\n"
                    + "「载入文件」= 用上面这些新配置刷新软件；\n"
                    + "「还原文件」= 用软件里现在的配置覆盖回文件，外部改动作废；\n"
                    + "「取消」= 两个都不做，先放着（文件再变会再问一次）。",
                    "载入文件", "还原文件");
                bool? r = dlg.ShowDialog();

                // 三个分支都把时间戳认下来：否则同一份文件会被反复判成「新改动」，弹个没完。
                if (dlg.AltChose)
                {
                    ProjectManager.SaveCurrent(name);
                    _known = LastWriteUtc(name);
                    StatusBarService.ReportInfo($"已用软件里当前的配置覆盖工程文件（外部改动作废）：{name}.xlsx");
                    return;
                }
                if (r != true)
                {
                    _known = LastWriteUtc(name);
                    StatusBarService.ReportInfo("已忽略这次外部改动：文件与软件各保持现状。");
                    return;
                }

                StatusBarService.ReportInfo(ProjectManager.ApplyProjectFromDisk(disk));
                _known = LastWriteUtc(name);
            }
            finally
            {
                _busy = false;
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
