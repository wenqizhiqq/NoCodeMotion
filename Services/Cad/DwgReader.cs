// Services/Cad/DwgReader.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Aspose.CAD;
using Aspose.CAD.FileFormats.Cad;
using Aspose.CAD.FileFormats.Cad.CadObjects;

namespace NoCodeMotion.Services.Cad
{
    /// <summary>
    /// 从 DWG / DXF 提取二维矢量几何（线段 + 文字标注），供 3D 仿真页以"平面布局"方式显示。
    ///
    /// 设计要点：
    ///  - 借助 Aspose.CAD 读取二进制 DWG（AutoCAD 各版本）与 DXF 文本，二者都返回 CadImage。
    ///  - 递归展开 CadInsertObject → CadBlockEntity（含嵌套块，带深度/访问保护），得到完整几何。
    ///  - 不调用 Image.Save 栅格化，因此不会触发 Aspose 的"Evaluation only"水印（仅栅格导出才有水印）。
    ///  - 输出为"图纸坐标"（X/Y，忽略 Z），由 WPF 侧负责归一化与 Z-up→Y-up 映射，便于在 DwgTest 中无 UI 验证。
    /// </summary>

    /// <summary>一条线段（图纸坐标，单位与源文件一致）。</summary>
    public sealed class DwgSegment
    {
        public double X1, Y1, X2, Y2;
    }

    /// <summary>一个文字标注位置与内容（图纸坐标）。</summary>
    public sealed class DwgLabel
    {
        public double X, Y;             // 插入点（图纸坐标）
        public string Text = "";
        public double Height;           // 图纸单位（原始文字高度）
        public double RotationDeg;      // 旋转角（度）
    }

    /// <summary>一次 DWG/DXF 导入结果。</summary>
    public sealed class DwgDrawing
    {
        public string SourceFile = "";
        public List<DwgSegment> Segments = new();
        public List<DwgLabel> Labels = new();
        public int RawEntityCount;      // 顶层实体数
        public int ExpandedSegmentCount;// 展开块后的线段数（含圆弧采样）
        public double MinX = double.MaxValue, MinY = double.MaxValue;
        public double MaxX = double.MinValue, MaxY = double.MinValue;
        public bool HasData => Segments.Count > 0 || Labels.Count > 0;

        /// <summary>取景用主图块包围盒（已自动剔除离群分离图块，例如被拖到远处的电缆表/明细）。</summary>
        public double FitMinX = double.MaxValue, FitMinY = double.MaxValue;
        public double FitMaxX = double.MinValue, FitMaxY = double.MinValue;
        public bool HasFit => FitMinX <= FitMaxX && (FitMaxX - FitMinX) > 1e-6 && (FitMaxY - FitMinY) > 1e-6;

        public void AddPoint(double x, double y)
        {
            if (x < MinX) MinX = x; if (x > MaxX) MaxX = x;
            if (y < MinY) MinY = y; if (y > MaxY) MaxY = y;
        }
    }

    public static class DwgReader
    {
        /// <summary>读取 DWG/DXF，返回展开后的二维矢量几何（线段 + 文字）。失败抛异常。</summary>
        public static DwgDrawing Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("DWG/DXF 路径为空", nameof(path));
            if (!System.IO.File.Exists(path))
                throw new System.IO.FileNotFoundException("DWG/DXF 文件不存在", path);

            var drawing = new DwgDrawing { SourceFile = path };

            using var img = (CadImage)Image.Load(path);
            drawing.RawEntityCount = img.Entities.Count();

            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Walk(img.Entities, img, Affine.Identity, drawing, 0, visiting);

            drawing.ExpandedSegmentCount = drawing.Segments.Count;
            if (!drawing.HasData)
            {
                // 没有可渲染几何：可能是纯栅格/仅 3D 实体——如实返回空，由调用方提示。
            }
            else
            {
                ComputeFitBounds(drawing);
            }
            return drawing;
        }

        // ===== 递归遍历实体 =====
        private static void Walk(IEnumerable<CadEntityBase> ents, CadImage img, Affine m,
            DwgDrawing d, int depth, HashSet<string> visiting)
        {
            if (depth > 24) return; // 防止异常块循环
            foreach (var e in ents)
            {
                if (e == null) continue;
                switch (e)
                {
                    case CadLine cl:
                        AddSeg(d, m, cl.FirstPoint.X, cl.FirstPoint.Y, cl.SecondPoint.X, cl.SecondPoint.Y);
                        break;

                    case CadLwPolyline pl:
                        var coords = pl.Coordinates;
                        if (coords == null || coords.Count < 2) break;
                        for (int i = 0; i < coords.Count - 1; i++)
                            AddSeg(d, m, coords[i].X, coords[i].Y, coords[i + 1].X, coords[i + 1].Y);
                        int flag = (int)pl.Flag;
                        if ((flag & 1) != 0) // 闭合
                            AddSeg(d, m, coords[coords.Count - 1].X, coords[coords.Count - 1].Y, coords[0].X, coords[0].Y);
                        break;

                    // 注意：CadArc 继承自 CadCircle，必须把 Arc 放在 Circle 之前，否则 Arc 会被 Circle 分支拦截
                    case CadArc ca:
                        {
                            double r = ca.Radius * m.LinearScale;
                            double start = ca.StartAngle, end = ca.EndAngle;
                            // DXF 弧默认逆时针；CounterClockwize != 0 表示反向
                            bool ccw = ca.CounterClockwize == 0;
                            double sweep = end - start;
                            if (ccw && sweep < 0) sweep += 2 * Math.PI;
                            if (!ccw && sweep > 0) sweep -= 2 * Math.PI;
                            int n = ArcSegCount(r, Math.Abs(sweep));
                            for (int i = 0; i < n; i++)
                            {
                                double a0 = start + sweep * i / n;
                                double a1 = start + sweep * (i + 1) / n;
                                AddSeg(d, m,
                                    ca.CenterPoint.X + ca.Radius * Math.Cos(a0), ca.CenterPoint.Y + ca.Radius * Math.Sin(a0),
                                    ca.CenterPoint.X + ca.Radius * Math.Cos(a1), ca.CenterPoint.Y + ca.Radius * Math.Sin(a1));
                            }
                        }
                        break;

                    case CadCircle cc:
                        {
                            double r = cc.Radius * m.LinearScale;
                            int n = CircleSegCount(r);
                            for (int i = 0; i < n; i++)
                            {
                                double a0 = 2 * Math.PI * i / n, a1 = 2 * Math.PI * (i + 1) / n;
                                AddSeg(d, m,
                                    cc.CenterPoint.X + cc.Radius * Math.Cos(a0), cc.CenterPoint.Y + cc.Radius * Math.Sin(a0),
                                    cc.CenterPoint.X + cc.Radius * Math.Cos(a1), cc.CenterPoint.Y + cc.Radius * Math.Sin(a1));
                            }
                        }
                        break;

                    case CadText tx:
                        {
                            double x, y; m.Map(tx.FirstAlignment.X, tx.FirstAlignment.Y, out x, out y);
                            d.Labels.Add(new DwgLabel
                            {
                                X = x, Y = y,
                                Text = tx.DefaultValue ?? "",
                                Height = tx.TextHeight * m.LinearScale,
                                RotationDeg = tx.TextRotation * 180.0 / Math.PI
                            });
                            d.AddPoint(x, y);
                        }
                        break;

                    case CadMText mt:
                        {
                            double x, y; m.Map(mt.InsertionPoint.X, mt.InsertionPoint.Y, out x, out y);
                            string t = !string.IsNullOrEmpty(mt.Text) ? mt.Text
                                     : (!string.IsNullOrEmpty(mt.FullText) ? mt.FullText : "");
                            d.Labels.Add(new DwgLabel
                            {
                                X = x, Y = y,
                                Text = t ?? "",
                                Height = mt.InitialTextHeight * m.LinearScale,
                                RotationDeg = mt.RotationAngleRad * 180.0 / Math.PI
                            });
                            d.AddPoint(x, y);
                        }
                        break;

                    case CadInsertObject ins:
                        ResolveInsert(ins, img, m, d, depth, visiting);
                        break;

                    // CadSpline / 3D 实体 / 尺寸标注等：当前跳过（不影响布局主结构）
                    default:
                        break;
                }
            }
        }

        // ===== 块插入：解析块定义并把其几何按插入变换递归展开 =====
        private static void ResolveInsert(CadInsertObject ins, CadImage img, Affine m,
            DwgDrawing d, int depth, HashSet<string> visiting)
        {
            string bname = ins.OriginalBlockName;
            if (string.IsNullOrEmpty(bname)) bname = ins.Name;
            if (string.IsNullOrEmpty(bname)) return;
            if (!img.BlockEntities.ContainsKey(bname)) return;
            var block = img.BlockEntities[bname];
            if (block == null) return;
            var blockEnts = block.Entities as IEnumerable<CadEntityBase>;
            if (blockEnts == null) return;

            // 插入变换：先去 BasePoint，再缩放，再绕 Z 旋转，最后平移到插入点
            var mBase = Affine.Translate(-block.BasePoint.X, -block.BasePoint.Y);
            var mScale = Affine.Scale(ins.ScaleX, ins.ScaleY);
            var mRot = Affine.Rotate(ins.RotationAngle);
            var mIns = Affine.Translate(ins.InsertionPoint.X, ins.InsertionPoint.Y);
            Affine mBlock = mIns.Then(mRot).Then(mScale).Then(mBase);

            int cols = ins.ColumnCount > 0 ? ins.ColumnCount : 1;
            int rows = ins.RowCount > 0 ? ins.RowCount : 1;
            double ang = ins.RotationAngle;
            double cs = Math.Cos(ang), sn = Math.Sin(ang);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double ox = c * ins.ColumnSpacing;
                    double oy = r * ins.RowSpacing;
                    // 行列偏移在插入局部坐标下，需按旋转角旋到世界系
                    double wx = ox * cs - oy * sn;
                    double wy = ox * sn + oy * cs;
                    Affine mo = Affine.Translate(wx, wy).Then(mBlock);

                    string key = bname + "|" + c + "|" + r;
                    if (!visiting.Add(key)) continue;
                    Walk(blockEnts, img, mo, d, depth + 1, visiting);
                    visiting.Remove(key);
                }
            }
        }

        // ===== 取景包围盒：网格连通分量聚类，挑选面积最大的图块作为主图 =====
        // 许多 CAD 会把明细表/电缆表/局部详图拖到远离主图的位置（本文件实测有一处远在 Y≈-2.5M，
        // 占 96% 图元），若直接按全局包围盒取景，主图会被压成不可见的小点。这里按拓扑邻接把图元分成
        // 若干分离图块，取面积最大的那块作为相机取景目标，同时保留全部图元用于渲染。
        private static void ComputeFitBounds(DwgDrawing d)
        {
            var pts = new List<(double x, double y)>(d.Segments.Count * 2 + d.Labels.Count);
            foreach (var s in d.Segments) { pts.Add((s.X1, s.Y1)); pts.Add((s.X2, s.Y2)); }
            foreach (var l in d.Labels) pts.Add((l.X, l.Y));
            if (pts.Count == 0) return;

            double gminX = double.MaxValue, gminY = double.MaxValue, gmaxX = double.MinValue, gmaxY = double.MinValue;
            foreach (var p in pts)
            {
                if (p.x < gminX) gminX = p.x; if (p.x > gmaxX) gmaxX = p.x;
                if (p.y < gminY) gminY = p.y; if (p.y > gmaxY) gmaxY = p.y;
            }
            double span = Math.Max(gmaxX - gminX, gmaxY - gminY);
            if (span <= 0) { SetFit(d, gminX, gminY, gmaxX, gmaxY); return; }

            double bin = Math.Max(1.0, span / 2000.0);
            var bins = new Dictionary<(int ix, int iy), List<int>>();
            for (int i = 0; i < pts.Count; i++)
            {
                int ix = (int)Math.Floor(pts[i].x / bin);
                int iy = (int)Math.Floor(pts[i].y / bin);
                var key = (ix, iy);
                if (!bins.TryGetValue(key, out var lst)) { lst = new List<int>(); bins[key] = lst; }
                lst.Add(i);
            }

            var visited = new HashSet<(int ix, int iy)>();
            (int, int)[] offs = { (-1,-1),(-1,0),(-1,1),(0,-1),(0,1),(1,-1),(1,0),(1,1) };
            double bestArea = -1;
            double fminX = 0, fminY = 0, fmaxX = 0, fmaxY = 0;

            foreach (var start in bins.Keys)
            {
                if (!visited.Add(start)) continue;
                var stack = new Stack<(int, int)>();
                stack.Push(start);
                double cminX = double.MaxValue, cminY = double.MaxValue, cmaxX = double.MinValue, cmaxY = double.MinValue;
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    foreach (var pi in bins[cur])
                    {
                        var p = pts[pi];
                        if (p.x < cminX) cminX = p.x; if (p.x > cmaxX) cmaxX = p.x;
                        if (p.y < cminY) cminY = p.y; if (p.y > cmaxY) cmaxY = p.y;
                    }
                    foreach (var o in offs)
                    {
                        var nk = (cur.Item1 + o.Item1, cur.Item2 + o.Item2);
                        if (bins.ContainsKey(nk) && visited.Add(nk)) stack.Push(nk);
                    }
                }
                double area = (cmaxX - cminX) * (cmaxY - cminY);
                if (area > bestArea) { bestArea = area; fminX = cminX; fminY = cminY; fmaxX = cmaxX; fmaxY = cmaxY; }
            }

            if (bestArea <= 0) SetFit(d, gminX, gminY, gmaxX, gmaxY);
            else SetFit(d, fminX, fminY, fmaxX, fmaxY);
        }

        private static void SetFit(DwgDrawing d, double x0, double y0, double x1, double y1)
        {
            d.FitMinX = x0; d.FitMinY = y0; d.FitMaxX = x1; d.FitMaxY = y1;
        }

        // ===== 工具 =====
        private static void AddSeg(DwgDrawing d, Affine m, double x1, double y1, double x2, double y2)
        {
            double ax, ay, bx, by;
            m.Map(x1, y1, out ax, out ay);
            m.Map(x2, y2, out bx, out by);
            d.Segments.Add(new DwgSegment { X1 = ax, Y1 = ay, X2 = bx, Y2 = by });
            d.AddPoint(ax, ay); d.AddPoint(bx, by);
        }

        private static int CircleSegCount(double r) => Math.Max(24, (int)Math.Ceiling(2 * Math.PI * r / 20.0));
        private static int ArcSegCount(double r, double sweep) => Math.Max(2, (int)Math.Ceiling(r * sweep / 20.0));

        /// <summary>2D 仿射变换 x'=a*x+b*y+e ; y'=c*x+d*y+f。</summary>
        private struct Affine
        {
            public double a, b, c, d, e, f;
            public Affine(double a, double b, double c, double d, double e, double f)
            { this.a = a; this.b = b; this.c = c; this.d = d; this.e = e; this.f = f; }
            public static Affine Identity => new Affine(1, 0, 0, 1, 0, 0);
            public static Affine Translate(double x, double y) => new Affine(1, 0, 0, 1, x, y);
            public static Affine Scale(double sx, double sy) => new Affine(sx, 0, 0, sy, 0, 0);
            public static Affine Rotate(double rad)
            {
                double cs = Math.Cos(rad), sn = Math.Sin(rad);
                return new Affine(cs, -sn, sn, cs, 0, 0);
            }
            /// <summary>this ∘ o：先应用 o，再应用 this。</summary>
            public Affine Then(Affine o) => new Affine(
                a * o.a + b * o.c, a * o.b + b * o.d, a * o.e + b * o.f + e,
                c * o.a + d * o.c, c * o.b + d * o.d, c * o.e + d * o.f + f);
            public void Map(double x, double y, out double ox, out double oy)
            { ox = a * x + b * y + e; oy = c * x + d * y + f; }
            public double LinearScale => Math.Sqrt(a * a + c * c);
        }
    }
}
