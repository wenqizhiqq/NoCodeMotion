// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温​启⁣志​◆‎编‌写⁠◇‍微⁠信‍﹕‎1⁣8​7‍◆‌1⁣9‍3‍6‏◇‌1‍3‌9⁣9⁣　‏※⁣保‍留⁠所‎有​权​利‎请⁣勿⁠删‎除⁠◇​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NoCodeMotion.Models.NodeGraph;
using NoCodeMotion.Services.Vision;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 工程 xlsx 里的「算子手册」页：把当前版本的所有算子 / 流程节点连同参数说明写进工程文件，
    /// AI（或人）打开工程就能查到「有哪些算子、每个参数怎么填」，不用另外找文档。
    /// <para>只读参考表：软件导入工程时会跳过这一页，改它不会影响工程配置。</para>
    /// </summary>
    public static class OperatorManualSheet
    {
        public const string SheetName = "算子手册";

        /// <summary>整本说明书：一行一个算子 / 节点。列固定（AI 按列名读）。</summary>
        public static DataTable Build()
        {
            var dt = new DataTable(SheetName);
            dt.Columns.Add("类型");                       // 读我 / 视觉算子 / 流程节点
            dt.Columns.Add("分类");
            dt.Columns.Add("名称");
            dt.Columns.Add("内部键");
            dt.Columns.Add("一句话说明");
            dt.Columns.Add("参数（名称 = 默认值｜白话说明）");
            dt.Columns.Add("输出 / 结果");
            dt.Columns.Add("怎么用");

            dt.Rows.Add(
                "读我", "全部", "算子手册（软件每次保存自动重建）", "",
                "这张表是当前版本软件里「能用的全部算子和流程节点」，含参数名、默认值和白话说明。改工程前先读这张表，流程里只能使用这里列出的名称。",
                "参数写法：参数名 = 默认值｜说明；一行一个参数，用换行分隔。",
                "视觉算子写进「视觉流程」的步骤（Op 内部键 + 参数 JSON）；流程节点写进「流程」页的节点图（Kind 内部键 + 属性）。",
                "示例：视觉算子「找直线」= line，参数「框选区域」= roi。节点「电机走位」= MoveAxis，参数「目标位置」= 0。");

            foreach (var op in VisionOperators.All2)
            {
                dt.Rows.Add(
                    "视觉算子",
                    op.Category,
                    op.Name,
                    op.Key,
                    op.Summary,
                    JoinParams(op.Params.Select(p => (p.Name, p.Default, p.Options, p.Help))),
                    op.Outputs,
                    "在「视觉流程」步骤的「分组 → 步骤类型」里选它；结果按「变量前缀」自动写成变量。");
            }

            foreach (var def in NgNodeDefinitions.All.Values)
            {
                var domain = NgNodeDefinitions.DomainTitle.TryGetValue(def.Domain, out var t) ? t : def.Domain.ToString();
                string name = def.Title;
                if (!string.IsNullOrWhiteSpace(def.StdName) && def.StdName != def.Title)
                    name += "（" + def.StdName + "）";
                dt.Rows.Add(
                    "流程节点",
                    domain,
                    name,
                    def.Kind.ToString(),
                    def.Summary,
                    JoinParams(def.Props.Select(p => (p.Name, p.Default, p.Options, p.Help))),
                    def.Outputs == null ? "" : string.Join(" / ", def.Outputs),
                    "在「流程」页的节点图里从工具箱拖出来；参数在右侧属性面板里填。");
            }

            return dt;
        }

        static string JoinParams(IEnumerable<(string Name, string Default, string Options, string Help)> ps)
        {
            var lines = new List<string>();
            foreach (var p in ps)
            {
                string one = p.Name + " = " + p.Default;
                if (!string.IsNullOrWhiteSpace(p.Options)) one += "（可选：" + p.Options + "）";
                if (!string.IsNullOrWhiteSpace(p.Help)) one += "｜" + p.Help;
                lines.Add(one);
            }
            return string.Join("\n", lines);
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓​⁣​
