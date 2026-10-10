# 通讯子系统（含真实 SECS/HSMS）
> 本文是 `MEMORY.md` 第十节的展开。改通讯、加协议、动 SECS 之前先读这一篇。
> 涉及文件：`Services/Hardware/Comm/**`、`Models/CommItem.cs`、`ViewModels/CommViewModel.cs`、`Views/CommPage.xaml`。

## 一、分层与数据流（一条通道，页面和 Lua 共用）

```
Lua: CommSend / CommRecv         页面: 打开/关闭/发送/接收/建立会话/探活
        │                                      │
   HardwareApi → IHardwareBridge.CommSend      │
        │        LeadshineHardwareBridge       │
        │        WenQiZhiCardBridge           │
        └──────────────┬───────────────────────┘
                       ▼
                 CommManager          按 ChannelKey 缓存，同一 COM/IP 不重复打开
                       │  Create(cfg) 按 CommType 分派
      ┌────────────────┼────────────────┬──────────────┬───────────────┐
 SerialCommChannel  TcpCommChannel  UdpCommChannel  ModbusCommChannel  SecsCommChannel
   (串口/RS232)     (网口TCP/相机)     (网口UDP)      (ModbusRTU/TCP)   (SECS(HSMS))
```

`ICommChannel` 只有 5 个成员：`Name` / `IsOpen` / `Open()` / `Send(string)` / `Recv()`（+ `IDisposable`）。
**页面与 Lua 走的是同一批实现** —— 页面不再是仿真。

`CommManager` 新增的三个成员（页面靠它们，别删）：
- `SendAndReply(cfg, data)` —— 发送并取回「立即应答」（SECS 的 W-bit 应答）；非 SECS 通道按普通发送处理并返回 `null`。
- `Peek(cfg)` —— 取通道，**不存在则创建**（会真跑 `Create`）。
- `TryPeek(cfg)` —— 取通道，**不存在返回 null，绝不创建**。
- `Close(cfg)` —— 关闭并从缓存移除。

## 二、三条最贵的坑

### ① 控制消息判定必须同时要求 `Stream(byte3) == 0`
HSMS 头 `[5]` 放的是：数据消息 = **功能号**，控制消息 = **控制码**。两者取值区间重叠 ——
`S1F1` 的功能号 `1` 和 `Select.req` 的 SType `1` 是**同一个字节值**。

```csharp
// ★ 正确：byte3 必须为 0，且 byte5 落在控制码集合里
bool isControl = hb3 == 0 && Array.IndexOf(ControlTypes, maybe) >= 0;
// ✗ 只看 byte5：S1F1 会被当成 Select.req，会话状态直接乱掉
```
冒烟段 S20/S22 就是钉这一条的。

### ② `CommManager.Create` 里 SECS 分支必须在最前
`Contains(type, "SECS", "HSMS", …)` 必须排在 Modbus / 串口 / UDP 之前，**尤其要排在最后的
「默认按 TCP 处理」之前**。否则 `SECS(HSMS)` 会掉进默认分支变成普通 `TcpCommChannel` ——
**不报错、不抛异常，只是完全没有 SECS 语义**，最难查。守卫 G28.21 断言三个锚点的下标顺序。

### ③ `SecsItem` 的静态工厂不能叫 `Ints` / `Reals`
`SecsItem` 已经有同名**实例属性** `List<long> Ints` / `List<double> Reals`（值承载）。
静态工厂再叫 `Ints` / `Reals` → **CS0102 编译错**。现名为 `Numbers(SecsFormat, params long[])` /
`Floats(SecsFormat, params double[])`；`U1/U2/U4/U8/I1/I2/I4/I8` 与 `F4/F8` 是它们的快捷包装。
守卫 G28.6 防止有人改回去。

## 三、SEMI E5：数据项编解码（`Secs/SecsItem.cs`）

- `SecsFormat` 枚举值**就是规范里那个 6 位格式码本身**：
  `L=0 B=8 BOOLEAN=9 A=16 JIS8=17 I8=24 I1=25 I2=26 I4=28 F8=32 F4=36 U8=40 U1=41 U2=42 U4=44`
  （规范用八进制书写，把八进制数字当二进制读即可）。
- 首字节 = `(格式码 << 2) | 长度字节数`：`A` + 1 字节长度 → `0x41`、`U1` → `0xA5`、`L` → `0x01`。
- **长度字段语义按格式分三种**：
  - `L` → **子项个数**（不是字节数！）
  - `A` / `JIS8` → 字节数
  - 其余 → 元素个数
- 长度字段 1/2/3 字节：`≤0xFF` 1 字节、`≤0xFFFF` 2 字节、否则 3 字节（大端）。
- 整型编码按 `ElementSize` 高字节序，解码时符号扩展 `unchecked((long)raw << shift) >> shift`。
- 文本侧是 SML：`<L <A "START"> <U4 1 2 3>>`、`<B 0x01 0x02>`、`<BOOLEAN TRUE FALSE>`、`<F8 1.5>`、空列表 `<>`。
  `ToSml()/ToSmlLine()` 与 `ParseSml()` 双向；冒烟段 S11–S16 逐格式对拍（二进制往返后 SML 文本逐字相同）。

## 四、SEMI E37：HSMS 会话（`Secs/SecsMessage.cs` + `Secs/HsmsSession.cs`）

**帧** = 4 字节大端长度前缀 + 10 字节头 + 可选数据体。

| 字节 | 数据消息 | 控制消息 |
|---|---|---|
| `[0..1]` | DeviceID | `0xFFFF` |
| `[2]` | W-bit（`0x80`） | 状态 / 拒绝原因 |
| `[3]` | Stream | `0` |
| `[4]` | PType `0`（SECS-II） | `0` |
| `[5]` | Function | SType |
| `[6..9]` | SystemBytes（大端） | SystemBytes |

**控制码**：`Select.req=1 Select.rsp=2 Deselect.req=3 Deselect.rsp=4 Linktest.req=5 Linktest.rsp=6 Reject.req=7 Separate.req=9`
—— **没有 `Separate.rsp`，也没有 8**。

**★ `Separate.req` / `Reject.req` 没有应答**，所以「要不要等应答」不能写 `msg.IsControl || msg.WBit`
（那样会给它们白等一个 T6 超时）。正确写法：
```csharp
bool waitReply = msg.IsData ? msg.WBit : msg.ReplyType().HasValue;   // ReplyType() 对无应答的返回 null
```

**两种角色**（`SecsRole`）：
- 被动（设备端，默认）：`TcpListener` 监听本机端口（`BaudOrPort` 填 `0` → 系统分配，实际端口看 `BoundPort`），
  等 Host 连入；`Open()` 立即返回。`IsListening` 表示正在监听。
- 主动（主机端）：连 `PortOrIp:BaudOrPort`，连上后**自己发 `Select.req` 并等 `Select.rsp`**；`Open()` 失败抛异常。

**超时**：`T3` 等数据应答（默认 45s）、`T5` 连接分离后重连/重听间隔（10s）、`T6` 等控制应答（5s）、
`T7` 连上但一直不 Select 的容忍（10s）、`T8` 网络字符间超时（5s，作 socket 读写超时）。

**线程模型**：被动有一条 accept 线程 + 每连接一条读线程；读线程负责分帧 → 解码 → 分发：
控制消息就地处理并自动应答；数据消息先看是不是「某条 W-bit 请求的应答」（按 SystemBytes 配对），
是就交回等待中的发送方；否则按 `AutoReply` 自动应答 `S1F1/S1F13/S1F15/S1F17`，再进收件队列供 `Recv()`。

**★ 刻意不依赖 `HardwareBridge` 的 `WaitGuard`**：通讯通道与运动控制互不阻塞，会话线程始终在后台跑，
页面/脚本的收发只是「入队/出队」。守卫 G28.16 钉这一条。

**自动应答表**（`BuildAutoReply`）：
`S1F1→S1F2 <L <A MDLN> <A SOFTREV>>`、`S1F13→S1F14 <L <B 0> <L <A MDLN> <A SOFTREV>>>`、
`S1F15→S1F16 <B 0>`、`S1F17→S1F18 <B 0>`。其余一律**不猜**，留给流程/脚本。

**单帧上限** `MaxFrameLength = 16 MB`；长度 < 10 或 > 上限 → 记 `LastError` 并**断开**（不把垃圾当报文）。

## 五、SECS 通道（`Secs/SecsCommChannel.cs`）

配置映射（`CommType = "SECS(HSMS)"`）：
- `SecsRole` → 主动 / 被动
- `PortOrIp` → 主动模式的对方 IP（被动模式忽略）
- `BaudOrPort` → 端口（HSMS 标准 5000；被动填 0 = 系统分配）
- `SecsDeviceId` / `SecsT3Ms` / `SecsT5Ms` / `SecsT6Ms` / `SecsT7Ms` / `SecsT8Ms` / `SecsAutoReply` / `SecsMdln` / `SecsSoftRev`

文本侧统一 SML（页面发送框与 Lua 都是）：
```
S1F1 W                                     → 在线查询，应答 S1F2
S2F41 W <L <A START> <L>>                  → 远程命令
S6F11 W <L <U4 1> <U4 1001> <A LOT01>>     → 事件上报
LINKTEST / SELECT / DESELECT / SEPARATE    → 控制消息
```

**★ W-bit 报文的应答会被会话层按 SystemBytes 配对吃掉，不会进收件队列** ——
所以通道额外提供 `SendAndReply(data)` 把应答文本交回调用方；页面「发送」与 `CommManager.SendAndReply` 都用它。
`Recv()` 只取「非请求消息」。

## 六、页面接线（`CommViewModel` + `CommPage.xaml`）

- 通讯类型下拉加了 `SECS(HSMS)`。**★ `CommTypeVisibilityConverter` 是精确匹配**（整串 `OrdinalIgnoreCase`），
  所以新类型必须加进**每一处**相关的 `ConverterParameter`：端口/IP 芯片、端口号芯片、SECS 参数卡、
  「建立会话」「探活」按钮。守卫 G28.33/G28.35 钉这一条。
- 旧行为已删：`OpenConnection` 只 `IsConnected = true`、`Send` 只回显（`以下为回显仿真`）。现在全部走 `CommManager`。
- **★ 日志必须 marshal 回 UI 线程**：HSMS 读线程会直接回调 `Log`，而 `DebugLog` 是
  `ObservableCollection`，跨线程改会在「对方一连上来」时抛 `InvalidOperationException`。用 `PostToUi`。
- 「接收」放后台线程（`Recv` 会阻塞到 `TimeoutMs`），日志 marshal 之后后台写日志是安全的。
- 刷状态用 `TryPeek`：`Peek` 会真 `Create`，而 S7 / 三菱MC 的 `Create` 直接抛 `NotSupportedException`
  → 只是「选中一条 S7 配置」就会当场抛异常。
- SECS 参数卡只在 `CommType = SECS(HSMS)` 时显示；「会话状态」回显 `监听中 0.0.0.0:端口 / TCP 已连接，等待 Select / HSMS 已选中`。

## 七、持久化

- `CommItem` 的 SECS 参数全是**标量属性** → `XlsxProjectStore` 反射导出/回填自动带上，进「通讯」表。
  老工程没有这些列 → 回填时属性保持默认值（导入按列名匹配，缺列不报错）。
- `AiProjectExchange.ApplyComms` 已补 `角色 / DeviceID / T3..T8 / 自动应答 / 机型 / 版本`
  （新增 `BoolDef` 助手，认 JSON 布尔也认 `1/0`、`true/false`、`是/否`）。
- `CommManager.ChannelKey` 已含 `SecsRole` + `SecsDeviceId` → 改了角色/设备号会重建通道，不会复用旧连接。

## 八、验证（守卫 G28 / 冒烟段 S）

- 守卫 `tools/guard_sources.py` G28.1–G28.37：枚举值、首字节公式、长度字段语义、控制消息判定、
  分支顺序、`TryPeek`、日志 marshal、页面 `ConverterParameter` 漂移、AI 交换字段。
- 冒烟 `.smoke/Program.cs` 段 S（S1–S57）：
  - S1–S13：全 15 种格式二进制往返 + 长度字段 1/2/3 字节边界（255/256/65535/65536）+ SML 往返；
  - S19–S24：头 10 字节精确值、S1F1 与 Select.req 的判定对照、PType≠0 失败、`ReplyType()`；
  - S25–S30：`CommManager` 分派 / `TryPeek` / S7 明确 `NotSupported` / `CommItem` 默认值；
  - **S31–S52：`127.0.0.1` 回环端到端** —— 被动监听（`Port=0` 拿 `BoundPort`）↔ 主动连接，
    `Select`、`S1F1→S1F2`（校验 MDLN/SOFTREV 就是配置值）、`S1F13→S1F14`、`S1F15→S1F16`、`S1F17→S1F18`、
    未请求 `S6F11` 走收件队列、`Linktest`、`Separate.req` 立即返回（0 ms）、非法帧长度断开、`Close()` 幂等；
  - S53–S57：通讯页离屏实例化 + SECS 卡片可见性关系（选中 SECS 可见 / 切回串口收起）+ 截图
    `.smoke/out/comm_secs_card.png`。
- 本机允许监听回环 → 段 S 不会 SKIP；若换到禁 socket 的环境，回环那一段会打印 `SKIP` 并跳过（不影响其余断言）。
