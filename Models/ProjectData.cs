// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‎启‌志⁣◆⁠编⁠写⁣◇‍微‎信⁠﹕⁠1⁣8‌7⁠◆‌1​9‎3‍6​◇‌1‌3‎9⁠9⁠　‌※‏保​留‏所‏有​权⁣利‎请⁠勿‌删‏除‌◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace NoCodeMotion.Models
{
    /// <summary>整个工程的配置数据：所有页面的列表都汇总在这里，作为唯一数据源（单一真实来源）。</summary>
    public class ProjectData
    {
        public ObservableCollection<AxisItem> Axes { get; set; } = new();

        /// <summary>控制器列表：每块运动控制卡/扩展IO模块，供轴页面选择归属。</summary>
        public ObservableCollection<AxisControllerItem> Controllers { get; set; } = new();
        public ObservableCollection<CylinderItem> Cylinders { get; set; } = new();
        public ObservableCollection<CommItem> Comms { get; set; } = new();
        public ObservableCollection<TrayItem> Trays { get; set; } = new();
        public ObservableCollection<FlowItem> Flows { get; set; } = new();

        /// <summary>相机列表：相机页/视觉流程共用。模板填充时直接在这里 Add 即可。</summary>
        public ObservableCollection<CameraItem> Cameras { get; set; } = new();

        /// <summary>点位表列表：一个点位表 = 一个工位，含该工位的 4 个轴与全部点位行。</summary>
        public ObservableCollection<PointTable> PointTables { get; set; } = new();

        /// <summary>【旧字段，仅用于兼容早期工程】单一点位表的点位行，载入后会迁移到 PointTables。</summary>
        public ObservableCollection<PointItem> Points { get; set; } = new();

        /// <summary>【旧字段，仅用于兼容早期工程】单一点位表所选的 4 个轴，载入后会迁移到 PointTables。</summary>
        public ObservableCollection<string> PointAxes { get; set; } = new();

        /// <summary>输入 IO 点位（左侧输入IO面板）</summary>
        public ObservableCollection<IoItem> Inputs { get; set; } = new();

        /// <summary>输出 IO 点位（右侧输出IO面板）</summary>
        public ObservableCollection<IoItem> Outputs { get; set; } = new();

        /// <summary>变量表（流程/逻辑中可引用的计算与状态变量），每行含 5 个 (名称/字符串值)。</summary>
        public ObservableCollection<VariableRow> Variables { get; set; } = new();

        /// <summary>自定义页面（可视化设计器）的控件列表：按钮 / 输入框 / 显示框，随工程落盘。</summary>
        public ObservableCollection<DesignerWidget> DesignerWidgets { get; set; } = new();

        /// <summary>需求文本：用户在项目管理页填写，用于「复制给 AI → 生成配置 → 粘贴回来」。\n        /// 多行字符串，每行一条需求或一段自然语言描述。</summary>
        public string RequirementsText { get; set; } = "";

        /// <summary>工程创建时间（首次保存时写入）。</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>工程最后修改时间（每次保存时更新）。</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>工程备注（自由文本，可在项目管理页编辑）。</summary>
        public string? Remark { get; set; }

        // === 兼容旧的 JSON 文件（保留一个旧字段 "Io"，反序列化时把内容迁移到 Inputs/Outputs） ===
        [JsonIgnore]
        public ObservableCollection<IoItem> Io
        {
            get => Inputs;
        }

        /// <summary>
        /// 载入工程后调用：把旧的单一点位表（Points / PointAxes）迁移为「工位1」，
        /// 并保证工程中至少存在一个点位表，同时补齐每个点位表的 4 个轴槽。
        /// </summary>
        public void EnsurePointTables()
        {
            if (PointTables.Count == 0)
            {
                var table = new PointTable { Name = "工位1" };
                for (int i = 0; i < PointTable.SlotCount && i < PointAxes.Count; i++)
                    table.AxisNames[i] = PointAxes[i];
                foreach (var p in Points)
                    table.Points.Add(p);
                PointTables.Add(table);
            }

            foreach (var t in PointTables)
            {
                t.EnsureAxisSlots();
                foreach (var p in t.Points) p.EnsureSlots();
            }

            // 旧字段已迁移完毕，清空避免下次载入重复迁移
            Points.Clear();
            PointAxes.Clear();

            MigrateAxisDefaults();
        }

        /// <summary>老工程里轴「运行速度 / 点动距离(寸动) / 手动速度」的默认值（100 / 1 / 20）。
        /// 旧版 AxisItem 用这三个数当默认值，落到 xlsx 里后会被当成「用户设定过的值」载回来，
        /// 于是新默认值 10000 对老工程不生效。</summary>
        private const double LegacyAxisSpeed = 100;
        private const double LegacyAxisJogStep = 1;
        private const double LegacyAxisManualSpeed = 20;

        /// <summary>新默认值：运行速度 / 点动距离 / 手动速度 一律 10000。</summary>
        public const double DefaultAxisSpeed = 10000;
        public const double DefaultAxisJogStep = 10000;
        public const double DefaultAxisManualSpeed = 10000;

        /// <summary>老工程里轴的「加速度 / 减速度」默认值 50。旧语义下它是「加速度值」，
        /// 新语义下 Accel/Decel 是**加速时间(秒)**——50 会被读成 50 秒斜坡，轴依然爬行。</summary>
        private const double LegacyAxisAccel = 50;

        /// <summary>新默认值：加速 / 减速时间 0.2 秒（0→额定速度的斜坡时长）。</summary>
        public const double DefaultAxisAccel = 0.2;

        /// <summary>
        /// 把轴的速度类参数迁移到新默认值（一次性、幂等）。
        /// <para>速度三项 → 10000；加减速 → 0.2 秒（语义由「加速度值」改为「加速时间(秒)」）。</para>
        /// <para>★ 只改「还等于旧默认值」的轴：用户亲手调过的值（如 3000、500）保持不动 ——
        /// 迁移的目标是「没设过的地方给个能动的默认」，不是覆盖用户设置。</para>
        /// <para>幂等性：迁移后值 ≠ 旧值，再跑一次自然不再命中。</para>
        /// </summary>
        public void MigrateAxisDefaults()
        {
            foreach (var a in Axes)
            {
                if (a == null) continue;
                if (a.Speed == LegacyAxisSpeed) a.Speed = DefaultAxisSpeed;
                if (a.JogStep == LegacyAxisJogStep) a.JogStep = DefaultAxisJogStep;
                if (a.ManualSpeed == LegacyAxisManualSpeed) a.ManualSpeed = DefaultAxisManualSpeed;
                // 加减速语义变更：旧的 50（被当加速度值）-> 新的 0.2 秒（加速时间）。
                // 只动「还等于旧默认值」的轴；用户若已把加减速设成别的数，说明他有自己的意图，保留。
                if (a.Accel == LegacyAxisAccel) a.Accel = DefaultAxisAccel;
                if (a.Decel == LegacyAxisAccel) a.Decel = DefaultAxisAccel;
            }
        }

        /// <summary>
        /// 原地复制：把 src 的全部集合内容复制到当前实例（清空后重新添加），
        /// 保留集合实例本身，使各页面 ViewModel 持有的集合引用仍然有效。
        /// 仅复制内容、不替换 ProjectData 实例；载入后由调用方再跑 EnsurePointTables 与名称库同步。
        /// </summary>
        public void CopyFrom(ProjectData src)
        {
            Axes.Clear(); foreach (var x in src.Axes) Axes.Add(x);
            Controllers.Clear(); foreach (var x in src.Controllers) Controllers.Add(x);
            Cylinders.Clear(); foreach (var x in src.Cylinders) Cylinders.Add(x);
            Comms.Clear(); foreach (var x in src.Comms) Comms.Add(x);
            Trays.Clear(); foreach (var x in src.Trays) Trays.Add(x);
            Flows.Clear(); foreach (var x in src.Flows) Flows.Add(x);
            Cameras.Clear(); foreach (var x in src.Cameras) Cameras.Add(x);
            PointTables.Clear(); foreach (var x in src.PointTables) PointTables.Add(x);
            Points.Clear(); foreach (var x in src.Points) Points.Add(x);
            PointAxes.Clear(); foreach (var x in src.PointAxes) PointAxes.Add(x);
            Inputs.Clear(); foreach (var x in src.Inputs) Inputs.Add(x);
            Outputs.Clear(); foreach (var x in src.Outputs) Outputs.Add(x);
            Variables.Clear(); foreach (var x in src.Variables) Variables.Add(x);
            DesignerWidgets.Clear(); foreach (var x in src.DesignerWidgets) DesignerWidgets.Add(x);
            RequirementsText = src.RequirementsText;
            CreatedAt = src.CreatedAt;
            UpdatedAt = src.UpdatedAt;
            Remark = src.Remark;
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
