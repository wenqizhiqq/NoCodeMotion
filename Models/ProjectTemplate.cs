// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温‏启‌志⁠◆⁣编⁠写‏◇⁠微‍信‌﹕⁠1‎8⁠7⁠◆​1⁠9‎3⁠6⁣◇‌1⁣3‏9⁣9‍　‍※⁠保‌留‎所‏有‌权‎利‏请‍勿‌删‌除‎◇⁠⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// =====================================================================
// 新建工程弹窗所用的「项目模板」模型。每个模板是一个工厂：
//   Build()  → 返回一份全新的 ProjectData（轴/IO/气缸/流程/点位 等都填好示例数据）。
//   同一模板被多次使用时，Build() 必须返回独立实例，避免模板被实例污染。
//
// 模板清单与具体内容由 Services/ProjectTemplateCatalog.cs 维护。
// =====================================================================
using System;
using System.Collections.Generic;

namespace NoCodeMotion.Models
{
    public class ProjectTemplate
    {
        /// <summary>模板唯一键（供持久化、查找、绑定回选用）。</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>显示名（弹窗左侧列表的主标题）。</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>分类（"空白" / "轴运动" / "气缸" / "IO" / "综合"）。</summary>
        public string Category { get; init; } = string.Empty;

        /// <summary>一行简介（弹窗右侧详情顶部）。</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>关键摘要（弹窗右侧芯片条，如 "1 轴 / 4 IO / 1 主流程 / 1 复位"）。</summary>
        public string Summary { get; init; } = string.Empty;

        /// <summary>亮点（弹窗右侧"包含什么"列表，项用换行分隔）。</summary>
        public IReadOnlyList<string> Highlights { get; init; } = Array.Empty<string>();

        /// <summary>工厂：每次返回全新 ProjectData 实例。默认返回空工程。</summary>
        public Func<ProjectData> Factory { get; init; } = () => new ProjectData();

        /// <summary>构建一份模板工程（永远返回新实例，调用方可放心修改）。</summary>
        /// <remarks>
        /// 在工厂产出的内容之上，额外挂两样示范内容：
        ///   1. 一份示范用的移动条件（Services/ProjectTemplateCatalog.SeedSampleConditions），
        ///      让「新建工程」出来的示例点位自带一套条件列表可供参考；
        ///   2. 一条「示例(节点图)」节点图流程（Services/ProjectTemplateCatalog.EnsureNodeGraphFlow），
        ///      让每个模板新建出来都能在流程页里看到节点图这类流程长什么样。
        /// 空白模板既没有点位表也没有轴 / IO / 相机，两道后处理都自然空转，保持「0 个流程」。
        /// </remarks>
        public ProjectData Build()
        {
            var data = Factory();
            Services.ProjectTemplateCatalog.SeedSampleConditions(data);
            Services.ProjectTemplateCatalog.EnsureNodeGraphFlow(data);
            return data;
        }
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣