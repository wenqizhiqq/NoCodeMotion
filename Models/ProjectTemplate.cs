// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦⁣
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇⁣
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
        public ProjectData Build() => Factory();
    }
}
// ◇作者保留所有权利　请勿删除※⁣
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓⁣