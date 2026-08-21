# -*- coding: utf-8 -*-
p = "ViewModels/FlowViewModel.cs"
t = open(p, "r", encoding="utf-8").read()

reps = []

# 1) add System.Globalization using
reps.append((
"using System.Windows.Threading;\nusing NoCodeMotion.Models;",
"using System.Windows.Threading;\nusing System.Globalization;\nusing NoCodeMotion.Models;"))

# 2) insert hardware-call block after the variable block, before "int next; switch"
old_block = '''            if (step.Function == "变量" && !string.IsNullOrWhiteSpace(step.Name))
            {
                if (step.Operation == "修改")
                    SetVariableValue(step.Name, step.SetValue);
                step.ActualValue = GetVariableValue(step.Name);
            }

            int next;
            switch (logic)'''
new_block = '''            if (step.Function == "变量" && !string.IsNullOrWhiteSpace(step.Name))
            {
                if (step.Operation == "修改")
                    SetVariableValue(step.Name, step.SetValue);
                step.ActualValue = GetVariableValue(step.Name);
            }

            // 真实硬件联动：功能为设备类且本行不是纯控制行时，把动作下发到机台（未挂真实桥走桩日志）
            if ((step.Function == "轴" || step.Function == "IO" || step.Function == "气缸" || step.Function == "modbus" || step.Function == "点位")
                && logic != "如果" && logic != "否则如果" && logic != "否则" && logic != "结束" && logic != "循环开始" && logic != "循环结束")
            {
                ExecuteHardwareStep(step);
            }

            int next;
            switch (logic)'''
reps.append((old_block, new_block))

# 3) insert ExecuteHardwareStep + ParseNum before FinishRun
methods = '''
        private static double ParseNum(string s, double def)
        {
            if (string.IsNullOrWhiteSpace(s)) return def;
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : def;
        }

        /// <summary>把流程里“设备类”步骤真实下发到机台：轴 / IO / 气缸 / modbus / 点位。</summary>
        private void ExecuteHardwareStep(FlowStep step)
        {
            var bridge = HardwareBridge.Current;
            try
            {
                switch (step.Function)
                {
                    case "轴":
                    {
                        var axis = HardwareResolver.ResolveAxis(step.Name);
                        if (axis == null) { bridge.Log($"找不到轴：{step.Name}"); break; }
                        string prop = (step.Property ?? string.Empty).Trim();
                        double val = ParseNum(step.SetValue, 0);
                        if (prop == "速度" || prop == "Speed") bridge.SetAxisSpeed(axis, val);
                        else if (prop == "回零" || prop == "Home" || prop == "原点") bridge.HomeAxis(axis);
                        else if (prop == "停止") bridge.StopAxis(axis);
                        else if (prop == "使能") bridge.EnableAxis(axis);
                        else if (!string.IsNullOrWhiteSpace(step.SetValue)) bridge.MoveAxisAbs(axis, val);
                        else bridge.MoveAxis(axis);
                        break;
                    }
                    case "IO":
                    {
                        var io = HardwareResolver.ResolveOutput(step.Name) ?? HardwareResolver.ResolveInput(step.Name);
                        if (io == null) { bridge.Log($"找不到 IO：{step.Name}"); break; }
                        if (HardwareResolver.ResolveOutput(step.Name) != null)
                            bridge.WriteOutput(io, ParseNum(step.SetValue, 0) >= 0.5 ? 1 : 0);
                        else
                            bridge.ReadInput(io);
                        break;
                    }
                    case "气缸":
                    {
                        var cyl = HardwareResolver.ResolveCylinder(step.Name);
                        if (cyl == null) { bridge.Log($"找不到气缸：{step.Name}"); break; }
                        string prop = (step.Property ?? string.Empty).Trim();
                        if (prop == "复位") { bridge.CylinderReset(cyl); break; }
                        int state = ParseNum(step.SetValue, 1) >= 0.5 ? 1 : 0;
                        if (prop == "缩回" || step.SetValue == "0") state = 0;
                        bridge.CylinderMove(cyl, state);
                        break;
                    }
                    case "modbus":
                    {
                        var comm = HardwareResolver.ResolveComm(step.Name);
                        if (comm == null) { bridge.Log($"找不到通讯：{step.Name}"); break; }
                        bridge.CommSend(comm, step.SetValue ?? string.Empty);
                        break;
                    }
                    case "点位":
                    {
                        var table = HardwareResolver.ResolvePointTable(step.Name);
                        if (table == null) { bridge.Log($"找不到点位表：{step.Name}"); break; }
                        foreach (var p in table.Points)
                        {
                            for (int i = 0; i < PointTable.SlotCount; i++)
                            {
                                var an = table.AxisNames.Count > i ? table.AxisNames[i] : string.Empty;
                                if (string.IsNullOrWhiteSpace(an)) continue;
                                var axis = HardwareResolver.ResolveAxis(an);
                                if (axis == null) continue;
                                var slot = p.Positions.Count > i ? p.Positions[i] : null;
                                if (slot == null) continue;
                                if (slot.Speed > 0) bridge.SetAxisSpeed(axis, slot.Speed);
                                bridge.MoveAxisAbs(axis, slot.Position);
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex) { bridge.Log($"硬件下发异常（{step.Name}）：{ex.Message}"); }
        }

'''
anchor = "        private void FinishRun()"
assert anchor in t, "FinishRun anchor not found"
t = t.replace(anchor, methods + anchor, 1)
reps.append(("__APPLIED_METHODS__", ""))  # placeholder to keep loop simple

ok = True
for k, (o, n) in enumerate(reps):
    if o == "__APPLIED_METHODS__":
        continue
    c = t.count(o)
    if c != 1:
        ok = False
        print(f"[FAIL] replacement #{k} count={c}")
        print("  old starts:", repr(o[:80]))
        idx = t.find(o[:40])
        print("  first occ at", idx)
        if idx >= 0:
            print("  context:", repr(t[idx - 30:idx + 120]))
        break
    t = t.replace(o, n, 1)

if ok:
    open(p, "w", encoding="utf-8").write(t)
    print("ALL OK, written. new length", len(t))
else:
    print("ABORTED")
