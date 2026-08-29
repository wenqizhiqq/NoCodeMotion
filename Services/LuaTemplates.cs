// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 脚本流程（Kind=Lua）各模板的预设 Lua 源码。
    /// 新建脚本流程时按所选模板写入 FlowItem.LuaSource，用户打开即可看到对应领域的示例脚本。
    ///
    /// ⚠ 只使用 HardwareApi.Register 真正注册的全局函数，否则脚本一运行就报错：
    ///   轴：AxisMove(axis) / SetAxisSpeed(axis,speed) / AxisHome(axis) / StopAxis(axis)
    ///       WaitAxisDone(axis) / EnableAxis(axis) / MoveAxisRel(axis,dist) / MoveAxisAbs(axis,pos)
    ///   IO：ReadIO(name)->number / WaitIO(name,value) / SetIO(name,value) / ToggleIO(name)
    ///   气缸：CylinderMove(name,value) / WaitCylinder(name) / CylinderReset(name)
    ///   通讯：CommSend(name,data) / CommRecv(name)->string
    ///   料盘：TrayPick(name) / TrayPlace(name)
    ///   硬件：HardwareStatus() / HardwareReady() / HardwareReconnect() / UseRealHardware() / UseSimulation()
    ///   （没有 File 读写 API，故「文件处理」模板改为纯 Lua 标准库的数据处理示例）
    /// </summary>
    public static class LuaTemplates
    {
        /// <summary>按模板名取预设 Lua 源码；未知模板或「空项目」回落到 FlowItem.DefaultLuaTemplate。</summary>
        public static string Get(string template) => template switch
        {
            "通讯" => Comm,
            "分拣" => Sort,
            "MES" => Mes,
            "文件处理" => DataProc,
            _ => FlowItem.DefaultLuaTemplate
        };

        // ---------------- 通讯 ----------------
        public const string Comm =
@"-- ============ 通讯脚本示例 ============
-- 向通讯端口发送指令 → 接收返回 → 解析数据
-- API：CommSend(通讯名, 数据) / CommRecv(通讯名)
local port = ""通讯1""

-- 1. 发送读取指令（Modbus RTU 读保持寄存器）
CommSend(port, ""01 03 00 00 00 02 C4 0B"")
print(""已发送读取指令 → "" .. port)

-- 2. 接收返回并解析
local resp = CommRecv(port)
if resp and #resp > 0 then
    print(""收到："" .. resp)
    -- 取第 4~5 字节换算为数值（按实际协议调整）
    local hex = string.sub(resp, 7, 10)
    local value = tonumber(hex, 16)
    print(""解析值 = "" .. tostring(value))
else
    print(""无返回数据，请检查通讯配置与接线"")
end

-- 3. 发送写入指令
CommSend(port, ""01 06 00 00 00 01 48 0A"")
print(""已发送写入指令"")
";

        // ---------------- 分拣 ----------------
        public const string Sort =
@"-- ============ 分拣脚本示例 ============
-- 读传感器 → 判断良品/不良品 → 气缸分拣 → 统计良率
-- API：ReadIO(输入点) / CylinderMove(气缸,1) / WaitCylinder(气缸) / CylinderReset(气缸)
local total, ok, ng = 0, 0, 0
local loops = 10

for i = 1, loops do
    local signal = ReadIO(""X0"")
    if signal == 1 then
        -- 良品 → 推入 A 料盒
        CylinderMove(""分拣缸A"", 1)
        WaitCylinder(""分拣缸A"")
        CylinderReset(""分拣缸A"")
        ok = ok + 1
        print(""第 "" .. i .. "" 个：OK  → A料盒"")
    else
        -- 不良品 → 推入 B 料盒
        CylinderMove(""分拣缸B"", 1)
        WaitCylinder(""分拣缸B"")
        CylinderReset(""分拣缸B"")
        ng = ng + 1
        print(""第 "" .. i .. "" 个：NG  → B料盒"")
    end
    total = total + 1
end

print(""分拣完成：总计="" .. total .. ""  OK="" .. ok .. ""  NG="" .. ng)
if total > 0 then
    print(string.format(""良率 = %.1f%%"", ok / total * 100))
end
";

        // ---------------- MES ----------------
        public const string Mes =
@"-- ============ MES 上报脚本示例 ============
-- 组装生产数据 → 上报 MES → 接收指令 → 按指令处理
-- API：CommSend(通讯名, 数据) / CommRecv(通讯名)
local mes    = ""通讯1""
local sn     = ""SN202608290001""
local result = ""PASS""
local cycle  = 3.2

-- 1. 组装并上报
local payload = string.format(""SN=%s;RESULT=%s;CT=%.1f"", sn, result, cycle)
CommSend(mes, payload)
print(""已上报 MES："" .. payload)

-- 2. 接收 MES 指令
local cmd = CommRecv(mes)
if cmd == ""CONTINUE"" then
    CommSend(mes, ""ACK"")
    print(""MES 允许继续生产，已回 ACK"")
elseif cmd == ""HOLD"" then
    print(""MES 要求暂停，请检查工艺参数"")
else
    print(""MES 返回："" .. tostring(cmd))
end

-- 3. 上报设备状态
CommSend(mes, ""STATUS=RUNNING"")
print(""已上报设备状态"")
";

        // ---------------- 数据处理（无文件 API，用 Lua 标准库） ----------------
        public const string DataProc =
@"-- ============ 数据处理脚本示例 ============
-- 说明：本软件未开放文件读写 API，故此处用 Lua 标准库（string/table）演示
--       数据行的解析、统计与筛选。实际使用时把 lines 换成从 CommRecv 取回的数据即可。
-- API：CommRecv(通讯名) 可用来获取外部数据

-- 1. 待处理数据行（格式：序号,数值A,数值B）
local lines = {
    ""1,100,25"",
    ""2,180,32"",
    ""3,95,18"",
    ""4,210,44"",
}
print(""待处理 "" .. #lines .. "" 行"")

-- 2. 逐行解析并累计
local results = {}
local sumA, sumB = 0, 0
for _, line in ipairs(lines) do
    local no, a, b = string.match(line, ""(%d+),(%d+),(%d+)"")
    if no and a and b then
        local sum = tonumber(a) + tonumber(b)
        results[#results + 1] = { no = no, a = a, b = b, sum = sum }
        sumA = sumA + tonumber(a)
        sumB = sumB + tonumber(b)
        print(""行 "" .. no .. ""："" .. a .. "" + "" .. b .. "" = "" .. sum)
    end
end

-- 3. 统计输出
print(string.format(""合计：A=%d  B=%d  有效行数=%d"", sumA, sumB, #results))

-- 4. 找出合计最大的一行
local maxRow = results[1]
for _, r in ipairs(results) do
    if r.sum > maxRow.sum then maxRow = r end
end
print(""最大行：序号="" .. maxRow.no .. ""  合计="" .. maxRow.sum)
";
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣