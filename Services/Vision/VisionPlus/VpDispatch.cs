// === NoCodeMotion 视觉流程引擎（VisionPlus 算子移植 · 分派表） | 作者：温启志 | 微信：18719361399 | 保留所有权利，请勿删除 ===
using System;
using System.Collections.Generic;
using Cv = OpenCvSharp;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision
{
    /// <summary>
    /// 新增算子的键 → 实现文件分派表。
    /// ★ Key 是落盘值，上线永不改名；新增算子只改这张表 + VisionOperators.cs 的 spec + 对应 Ops 文件。
    /// </summary>
    internal static class VpKeys
    {
        static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
        {
            // ---- 2D 基础 / 几何 / 图像统计 ----
            { "fixture",       "2d" },
            { "caliper",       "2d" },
            { "sharpness",     "2d" },
            { "similarity",    "2d" },
            { "geomcreate",    "2d" },
            { "geomintersect", "2d" },
            { "geomguide",     "2d" },
            { "hommat",        "2d" },
            { "morphchain",    "2d" },

            // ---- 匹配 / 颜色 / 训练式工具 / 标定 ----
            { "colormatch",    "match" },
            { "pminspect",     "match" },
            { "pmalign",       "match" },
            { "easyline",      "match" },
            { "easycircle",    "match" },
            { "colorlut",      "match" },
            { "pixmap16",      "match" },
            { "handeye",       "match" },
            { "npointcal",     "match" },

            // ---- 3D / 深度图 ----
            { "zmapload",      "3d" },
            { "zmap2img",      "3d" },
            { "planefit",      "3d" },
            { "heightcalc",    "3d" },
            { "volumecalc",    "3d" },
            { "anglepp",       "3d" },
            { "thickness",     "3d" },

            // ---- 深度学习 / 传统学习 / 读码 ----
            { "dlclassify",    "ai" },
            { "dldetect",      "ai" },
            { "dlsegment",     "ai" },
            { "dlocr",         "ai" },
            { "elclassify",    "ai" },
            { "dmcode",        "code" },
            { "codegrade",     "code" },
        };

        public static bool Has(string key) => !string.IsNullOrEmpty(key) && Map.ContainsKey(key);
        public static string Of(string key) => key != null && Map.TryGetValue(key, out var f) ? f : "";
    }

    /// <summary>
    /// 统一入口。VisionOpRunner 的 default 分支把没命中的老 79 个 case 转到这里；
    /// 异常由 VisionOpRunner 外层 try 统一兜底，本类不吞异常。
    /// </summary>
    internal static class VpDispatch
    {
        public static bool CanRun(string key) => VpKeys.Has(key);

        /// <summary>返回「之后的当前图像」：换图类算子返回自己新建的 Mat，其余返回 cur。</summary>
        public static Cv.Mat Run(VisionOpSpec spec, VisualFlowStep s, Cv.Mat cur, Cv.Mat gray, Cv.Mat dst,
            VisionReport report, VisionOpContext ctx, IProgress<string>? progress)
        {
            switch (VpKeys.Of(spec.Key))
            {
                case "2d": return VpOps2D.Run(spec, s, cur, gray, dst, report, ctx, progress);
                case "match": return VpOpsMatch.Run(spec, s, cur, gray, dst, report, ctx, progress);
                case "3d": return VpOps3D.Run(spec, s, cur, gray, dst, report, ctx, progress);
                case "ai": return VpOpsAI.Run(spec, s, cur, gray, dst, report, ctx, progress);
                case "code": return VpOpsCode.Run(spec, s, cur, gray, dst, report, ctx, progress);
                default: return cur;
            }
        }
    }
}
