// Services/Cad/DxfImporter.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NoCodeMotion.Services.Cad
{
    /// <summary>
    /// 极简 DXF（AutoCAD Drawing Exchange Format）文本导入器。
    /// 解析 ENTITIES 段，提取 LINE / LWPOLYLINE / POLYLINE / ARC / CIRCLE 几何，
    /// 按 HEADER $INSUNITS 换算到毫米，再沿每条实体按 stepMm 步长采样，得到一条有序 (X, Y) 路径。
    ///
    /// 支持实体：
    ///   LINE         ：[起点, 终点]；零长度线直接跳过
    ///   LWPOLYLINE   ：按顺序输出顶点；70 bit0=1（闭合）时追加首顶点收口
    ///   POLYLINE     ：嵌套 VERTEX 的 10/20 坐标；同样识别 70 闭合位（通过首顶点追加）
    ///   ARC          ：沿 CCW 弧线采样（弧长/stepMm 段），含起止角端点
    ///   CIRCLE       ：按周长/stepMm 段采样整圆
    ///
    /// 单位（HEADER $INSUNITS）：1=in→25.4，4=mm，5=cm→10，6=m→1000，其他→默认 mm。
    /// 不支持（静默跳过）：SPLINE / ELLIPSE / 3D 实体的 Z 坐标 / 文本/图层/块。
    /// 坐标值与实体类型名均为 ASCII，故用 UTF-8 读全部几何无编码问题。
    /// </summary>
    public static class DxfImporter
    {
        /// <summary>导入 DXF 并采样为有序 (X, Y) 路径点列（已统一到毫米）。</summary>
        /// <param name="filePath">DXF 文件路径</param>
        /// <param name="stepMm">采样步长（毫米），越小越密。≤0 时回退 5mm。</param>
        public static IReadOnlyList<(double X, double Y)> ImportToPoints(string filePath, double stepMm = 5.0)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("DXF 路径为空", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("DXF 文件不存在", filePath);
            if (stepMm <= 0) stepMm = 5.0;

            var (entities, scale) = Parse(filePath);
            return Sample(entities, scale, stepMm);
        }

        // ===== 内部解析结构 =====
        private enum Kind { Line, LwPolyline, Polyline, Arc, Circle, Skip }
        private struct V { public double X, Y; }
        private class E
        {
            public Kind Kind = Kind.Skip;
            public V A, B;                  // LINE: 起点/终点；ARC/CIRCLE: 圆心
            public double Radius;
            public double StartDeg, EndDeg;  // ARC：起/止角（度）
            public List<V> Verts = new();
            public bool IsClosed;            // LWPOLYLINE/POLYLINE 70 bit0
        }

        private static (List<E> entities, double scale) Parse(string path)
        {
            var list = new List<E>();
            double scale = 1.0;
            bool inHeader = false, inEntities = false;
            E? cur = null;      // 当前接收数据码的实体
            E? poly = null;     // 当前正在累积 VERTEX 数据的 POLYLINE
            bool expectInsUnits = false;
            int gc = -1;

            // DXF 坐标/实体名/段名均为 ASCII，UTF-8 读全部几何无编码问题。
            var lines = File.ReadAllLines(path);
            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li]!.Trim();
                if (line.Length == 0) continue;

                if (gc < 0)
                {
                    // 偶数行：组码（整数）
                    if (!int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out gc))
                        continue;
                    continue;
                }

                // 奇数行：值
                string val = line.Trim();
                int code = gc;
                gc = -1;

                // HEADER $INSUNITS 跟踪：DXF 头变量名用组码 9；先记录「等下一个 70」
                if (code == 9 && inHeader && val.Equals("$INSUNITS", StringComparison.OrdinalIgnoreCase))
                {
                    expectInsUnits = true;
                    continue;
                }
                if (expectInsUnits && code == 70)
                {
                    if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int u))
                        scale = InsUnitsScale(u);
                    expectInsUnits = false;
                    continue;
                }

                // 段名：SECTION 之后紧跟组码 2 的段名（HEADER / ENTITIES / ...）
                if (code == 2)
                {
                    if (val.Equals("HEADER", StringComparison.OrdinalIgnoreCase))
                    { inHeader = true; inEntities = false; }
                    else if (val.Equals("ENTITIES", StringComparison.OrdinalIgnoreCase))
                    { inEntities = true; inHeader = false; }
                    else { inHeader = false; inEntities = false; }
                    cur = null; poly = null;
                    continue;
                }

                // 段切换 / 实体类型（组码 0）
                if (code == 0)
                {
                    if (val.Equals("SECTION", StringComparison.OrdinalIgnoreCase))
                    { inHeader = inEntities = false; cur = null; poly = null; continue; }
                    if (val.Equals("ENDSEC", StringComparison.OrdinalIgnoreCase))
                    { inHeader = inEntities = false; cur = null; poly = null; continue; }

                    if (inEntities)
                    {
                        switch (val.ToUpperInvariant())
                        {
                            case "LINE":
                                cur = new E { Kind = Kind.Line }; list.Add(cur); break;
                            case "LWPOLYLINE":
                                cur = new E { Kind = Kind.LwPolyline }; list.Add(cur); break;
                            case "POLYLINE":
                                cur = new E { Kind = Kind.Polyline }; poly = cur; list.Add(cur); break;
                            case "VERTEX":
                                // 把 VERTEX 的 10/20 路由进当前 POLYLINE
                                cur = poly;
                                break;
                            case "SEQEND":
                                cur = null; poly = null; break;
                            case "ARC":
                                cur = new E { Kind = Kind.Arc }; list.Add(cur); break;
                            case "CIRCLE":
                                cur = new E { Kind = Kind.Circle }; list.Add(cur); break;
                            default:
                                cur = null; break;
                        }
                    }
                    else
                    {
                        cur = null;
                    }
                    continue;
                }

                if (cur == null) continue;

                // 数据码分发
                switch (cur.Kind)
                {
                    case Kind.Line:
                        if (code == 10) cur.A.X = D(val);
                        else if (code == 20) cur.A.Y = D(val);
                        else if (code == 11) cur.B.X = D(val);
                        else if (code == 21) cur.B.Y = D(val);
                        break;
                    case Kind.LwPolyline:
                        if (code == 10) cur.Verts.Add(new V { X = D(val) });
                        else if (code == 20) { if (cur.Verts.Count > 0) { var v = cur.Verts[cur.Verts.Count - 1]; v.Y = D(val); cur.Verts[cur.Verts.Count - 1] = v; } }
                        else if (code == 70)
                        {
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f)
                                && (f & 1) != 0) cur.IsClosed = true;
                        }
                        break;
                    case Kind.Polyline:
                        // VERTEX 的 10/20 与 POLYLINE 头里的 10/20 都汇到这里
                        if (code == 10) cur.Verts.Add(new V { X = D(val) });
                        else if (code == 20) { if (cur.Verts.Count > 0) { var v = cur.Verts[cur.Verts.Count - 1]; v.Y = D(val); cur.Verts[cur.Verts.Count - 1] = v; } }
                        else if (code == 70)
                        {
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f)
                                && (f & 1) != 0) cur.IsClosed = true;
                        }
                        break;
                    case Kind.Arc:
                    case Kind.Circle:
                        if (code == 10) cur.A.X = D(val);
                        else if (code == 20) cur.A.Y = D(val);
                        else if (code == 40) cur.Radius = D(val);
                        if (cur.Kind == Kind.Arc)
                        {
                            if (code == 50) cur.StartDeg = D(val);
                            else if (code == 51) cur.EndDeg = D(val);
                        }
                        break;
                }
            }
            return (list, scale);
        }

        private static IReadOnlyList<(double X, double Y)> Sample(List<E> entities, double scale, double stepMm)
        {
            var out_ = new List<(double, double)>();
            foreach (var e in entities)
            {
                switch (e.Kind)
                {
                    case Kind.Line:
                        if (Math.Abs(e.A.X - e.B.X) < 1e-9 && Math.Abs(e.A.Y - e.B.Y) < 1e-9)
                            break; // 零长度线跳过
                        out_.Add((e.A.X * scale, e.A.Y * scale));
                        out_.Add((e.B.X * scale, e.B.Y * scale));
                        break;
                    case Kind.LwPolyline:
                    case Kind.Polyline:
                        foreach (var v in e.Verts)
                            out_.Add((v.X * scale, v.Y * scale));
                        if (e.IsClosed && e.Verts.Count > 0)
                            out_.Add((e.Verts[0].X * scale, e.Verts[0].Y * scale));
                        break;
                    case Kind.Arc:
                        SampleArc(e.A.X * scale, e.A.Y * scale, e.Radius * scale,
                                  e.StartDeg, e.EndDeg, stepMm, out_);
                        break;
                    case Kind.Circle:
                        SampleCircle(e.A.X * scale, e.A.Y * scale, e.Radius * scale, stepMm, out_);
                        break;
                }
            }
            return out_;
        }

        private static void SampleArc(double cx, double cy, double r,
                                      double startDeg, double endDeg, double stepMm,
                                      List<(double, double)> out_)
        {
            if (r <= 0) return;
            // DXF ARC 永远 CCW：end < start 视为跨过 360°
            double sweep = endDeg - startDeg;
            if (sweep <= 0) sweep += 360;
            double arcLen = sweep / 360.0 * 2 * Math.PI * r;
            int n = Math.Max(2, (int)Math.Ceiling(arcLen / stepMm));
            for (int i = 0; i <= n; i++)
            {
                double a = (startDeg + sweep * i / n) * Math.PI / 180.0;
                out_.Add((cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
            }
        }

        private static void SampleCircle(double cx, double cy, double r, double stepMm,
                                         List<(double, double)> out_)
        {
            if (r <= 0) return;
            double circ = 2 * Math.PI * r;
            int n = Math.Max(8, (int)Math.Ceiling(circ / stepMm));
            for (int i = 0; i < n; i++)
            {
                double a = 2 * Math.PI * i / n;
                out_.Add((cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
            }
        }

        private static double D(string s)
            => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>DXF $INSUNITS → mm 换算系数。0/未识别默认 1（按毫米处理）。</summary>
        private static double InsUnitsScale(int code) => code switch
        {
            1 => 25.4,     // inches
            2 => 304.8,    // feet
            4 => 1.0,      // mm
            5 => 10.0,     // cm
            6 => 1000.0,   // m
            _ => 1.0
        };
    }
}