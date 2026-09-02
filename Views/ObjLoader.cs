// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;

namespace NoCodeMotion.Views
{
    /// <summary>
    /// 极简 WaveFront .obj 解析器（无第三方依赖）。
    /// 将 .obj 解析为 WPF <see cref="MeshGeometry3D"/>，保留原始坐标（不做单模型归一化），
    /// 以便多零件在共享 CAD 坐标系下正确装配。
    /// 支持：v / vn / vt / f（三角面、四边形、多边形自动三角化；顶点索引支持 v、v/vt、v/vt/vn 形式）。
    /// 当 .obj 缺少法线时，按面计算平面法线（平面着色），保证光照正确。
    /// </summary>
    public static class ObjLoader
    {
        public static MeshGeometry3D LoadFile(string path)
        {
            using var fs = File.OpenRead(path);
            return Load(fs);
        }

        public static MeshGeometry3D Load(Stream stream)
        {
            var verts = new List<Point3D>(1024);
            var pos = new List<Point3D>(4096);
            var nrm = new List<Vector3D>(4096);

            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == '#') continue;
                // 用空格切分，跳过空段
                var sp = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (sp.Length == 0) continue;

                switch (sp[0])
                {
                    case "v":
                        if (sp.Length < 4) continue;
                        verts.Add(ParsePoint(sp));
                        break;
                    case "f":
                        AddFace(sp, verts, pos, nrm);
                        break;
                    // vn / vt / g / o / usemtl / mtllib / s / l 等忽略
                    default:
                        break;
                }
            }

            var mesh = new MeshGeometry3D();
            foreach (var p in pos) mesh.Positions.Add(p);
            foreach (var n in nrm) mesh.Normals.Add(n);
            if (mesh.Positions.Count > 0) mesh.Freeze();
            return mesh;
        }

        private static Point3D ParsePoint(string[] sp)
        {
            return new Point3D(
                double.Parse(sp[1], CultureInfo.InvariantCulture),
                double.Parse(sp[2], CultureInfo.InvariantCulture),
                double.Parse(sp[3], CultureInfo.InvariantCulture));
        }

        private static void AddFace(string[] sp, List<Point3D> verts, List<Point3D> pos, List<Vector3D> nrm)
        {
            if (sp.Length < 4) return; // 至少需要 3 个顶点
            // 收集位置索引（1-based）
            int n = sp.Length - 1;
            var idx = new int[n];
            for (int i = 0; i < n; i++)
            {
                var tok = sp[i + 1];
                int slash = tok.IndexOf('/');
                string vi = slash >= 0 ? tok.Substring(0, slash) : tok;
                if (!int.TryParse(vi, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) || v == 0)
                    return; // 出现无法解析的索引则跳过该面（容错）
                idx[i] = v < 0 ? verts.Count + v : v - 1; // 支持负索引（相对）
                if (idx[i] < 0 || idx[i] >= verts.Count) return;
            }
            // 扇形三角化
            for (int i = 1; i + 1 < n; i++)
            {
                var a = verts[idx[0]];
                var b = verts[idx[i]];
                var c = verts[idx[i + 1]];
                var normal = Vector3D.CrossProduct(b - a, c - a);
                if (normal.Length < 1e-9) normal = new Vector3D(0, 0, 1);
                normal.Normalize();
                pos.Add(a); pos.Add(b); pos.Add(c);
                nrm.Add(normal); nrm.Add(normal); nrm.Add(normal);
            }
        }
    }
}
