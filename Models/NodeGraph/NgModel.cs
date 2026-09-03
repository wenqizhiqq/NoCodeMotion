// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۤ
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇
// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۤ
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoCodeMotion.Models.NodeGraph;

/// <summary>节点属性项（POCO，值可随属性面板编辑）。</summary>
public sealed class NgProp
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    /// <summary>可选项 "A|B|C"；为空表示自由文本。</summary>
    public string? Options { get; set; }
}

/// <summary>节点图节点（POCO，坐标与属性均落盘）。</summary>
public sealed class NgNode
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public NgKind Kind { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public List<NgProp> Props { get; set; } = new();
}

/// <summary>节点连线（分支 = 同一源节点多个输出端口对应不同连线）。</summary>
public sealed class NgConnection
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string SourceId { get; set; } = "";
    public string SourcePort { get; set; } = "Out";
    public string TargetId { get; set; } = "";
}

/// <summary>整个节点图文档（序列化到 FlowItem.GraphJson）。</summary>
public sealed class NgDoc
{
    public List<NgNode> Nodes { get; set; } = new();
    public List<NgConnection> Connections { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static NgDoc FromJson(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new NgDoc();
        try
        {
            var doc = JsonSerializer.Deserialize<NgDoc>(s!, JsonOptions);
            return doc ?? new NgDoc();
        }
        catch
        {
            return new NgDoc();
        }
    }
}

/// <summary>节点图几何常量与贝塞尔连线计算（供 ViewModel 渲染连线用）。</summary>
public static class NgGeometry
{
    public const double NodeWidth = 186;
    public const double HeaderHeight = 30;
    public const double OutputRowHeight = 22;
    public const double PortRadius = 6;

    /// <summary>输入端口锚点（节点左侧、标题栏垂直中心）。</summary>
    public static System.Windows.Point InputPoint(double x, double y) => new(x, y + HeaderHeight / 2);

    /// <summary>输出端口锚点（节点右侧，紧贴标题栏下方、按端口序号向下排列；
    /// 与 NodeGraphNodeView 中「输出端口行高固定 22、紧接标题栏」的布局一致，保证连线端点落在圆上）。</summary>
    public static System.Windows.Point OutputPoint(double x, double y, int portIndex)
        => new(x + NodeWidth, y + HeaderHeight + 11 + portIndex * OutputRowHeight);

    /// <summary>由两端点生成三次贝塞尔路径（水平方向相切，曲线自然）。</summary>
    public static System.Windows.Media.Geometry MakeBezier(System.Windows.Point p0, System.Windows.Point p1)
    {
        double dx = System.Math.Max(40, System.Math.Abs(p1.X - p0.X) / 2);
        var fig = new System.Windows.Media.PathFigure { StartPoint = p0 };
        fig.Segments.Add(new System.Windows.Media.BezierSegment(
            new System.Windows.Point(p0.X + dx, p0.Y),
            new System.Windows.Point(p1.X - dx, p1.Y),
            p1, true));
        return new System.Windows.Media.PathGeometry { Figures = new System.Windows.Media.PathFigureCollection { fig } };
    }

    /// <summary>箭头三角（指向 target 端）。</summary>
    public static System.Windows.Media.PointCollection MakeArrow(System.Windows.Point tip, System.Windows.Point from)
    {
        double ang = System.Math.Atan2(tip.Y - from.Y, tip.X - from.X);
        var b = new System.Windows.Point(tip.X - 11 * System.Math.Cos(ang - 0.4), tip.Y - 11 * System.Math.Sin(ang - 0.4));
        var c = new System.Windows.Point(tip.X - 11 * System.Math.Cos(ang + 0.4), tip.Y - 11 * System.Math.Sin(ang + 0.4));
        return new System.Windows.Media.PointCollection { tip, b, c };
    }
}

/// <summary>节点图流程模板：返回可直接写入 FlowItem.GraphJson 的 JSON 字符串。</summary>
public static class NgTemplates
{
    /// <summary>按模板名构建默认节点图（含预连接）。空项目仅放一个开始节点。</summary>
    public static string Build(string template)
    {
        var doc = new NgDoc();
        NgNode Node(NgKind kind, double x, double y)
        {
            var def = NgNodeDefinitions.All[kind];
            var n = new NgNode { Kind = kind, X = x, Y = y };
            foreach (var pd in def.Props)
                n.Props.Add(new NgProp { Name = pd.Name, Value = pd.Default, Options = pd.Options });
            doc.Nodes.Add(n);
            return n;
        }
        void Link(NgNode s, string port, NgNode t)
            => doc.Connections.Add(new NgConnection { SourceId = s.Id, SourcePort = port, TargetId = t.Id });

        if (template == "空项目")
        {
            Node(NgKind.Start, 80, 80);
            return doc.ToJson();
        }
        if (template == "通用流程")
        {
            var s = Node(NgKind.Start, 80, 80);
            var m = Node(NgKind.MoveAxis, 360, 80);
            var e = Node(NgKind.End, 640, 80);
            Link(s, "Out", m);
            Link(m, "Out", e);
            return doc.ToJson();
        }
        if (template == "设备启动")
        {
            var s = Node(NgKind.Start, 80, 60);
            var h = Node(NgKind.Home, 360, 60);
            var w = Node(NgKind.WaitInput, 360, 200);
            var d = Node(NgKind.Delay, 640, 60);
            var e = Node(NgKind.End, 920, 60);
            Link(s, "Out", h);
            Link(h, "Out", w);
            Link(h, "Out", d);
            Link(d, "Out", e);
            return doc.ToJson();
        }
        // 默认：仅开始节点
        Node(NgKind.Start, 80, 80);
        return doc.ToJson();
    }
}
// ◇作者保留所有权利　请勿删除※
// ◆◇※▣▤▥ۦ▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥ۦ▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤
