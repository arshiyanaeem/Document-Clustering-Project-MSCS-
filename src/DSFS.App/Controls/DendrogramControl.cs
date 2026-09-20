using DSFS.Core.Models;

namespace DSFS.App.Controls;

/// <summary>
/// Draws the dendrogram produced by <see cref="DSFS.Core.Clustering.AgglomerativeClustering"/>
/// (leaves at the bottom, merges rising to the root) using plain GDI+, matching the
/// "Document Clusters" visual output box in Figure 7 of the thesis.
/// </summary>
public class DendrogramControl : Control
{
    private ClusterNode? _root;
    private Dictionary<int, string> _leafLabels = new();
    private readonly Dictionary<int, double> _xById = new();

    public DendrogramControl()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        AutoScroll = true;
    }

    public void SetData(ClusterNode root, Dictionary<int, string> leafLabelsByDocumentId)
    {
        _root = root;
        _leafLabels = leafLabelsByDocumentId;
        _xById.Clear();
        int counter = 0;
        AssignLeafPositions(root, ref counter);
        Invalidate();
    }

    private void AssignLeafPositions(ClusterNode node, ref int counter)
    {
        if (node.IsLeaf)
        {
            _xById[node.Id] = counter++;
            return;
        }
        AssignLeafPositions(node.Left!, ref counter);
        AssignLeafPositions(node.Right!, ref counter);
        _xById[node.Id] = (_xById[node.Left!.Id] + _xById[node.Right!.Id]) / 2.0;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_root is null) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int leafCount = CountLeaves(_root);
        int marginLeft = 20, marginRight = 20, marginTop = 20, marginBottom = 60;
        int width = Math.Max(Width - marginLeft - marginRight, leafCount * 90);
        int height = Height - marginTop - marginBottom;

        double maxDistance = Math.Max(_root.MergeDistance, 0.0001);

        using var linePen = new Pen(Color.FromArgb(0, 120, 212), 1.6f);
        using var font = new Font("Segoe UI", 8f);
        using var textBrush = new SolidBrush(Color.Black);

        double xStep = leafCount > 1 ? (double)width / (leafCount - 1) : width;

        double XPos(double normalizedX) => marginLeft + normalizedX * xStep;
        double YPos(double distance) => marginTop + height - (distance / maxDistance) * height;

        void DrawNode(ClusterNode node)
        {
            if (node.IsLeaf)
            {
                double x = XPos(_xById[node.Id]);
                double y = YPos(0);
                string label = _leafLabels.TryGetValue(node.DocumentId, out var name) ? name : $"Doc{node.DocumentId}";
                g.DrawString(label, font, textBrush, (float)x - 6, (float)(y + 4));
                return;
            }

            DrawNode(node.Left!);
            DrawNode(node.Right!);

            double xLeft = XPos(_xById[node.Left!.Id]);
            double xRight = XPos(_xById[node.Right!.Id]);
            double xMid = XPos(_xById[node.Id]);
            double yLeft = YPos(node.Left!.MergeDistance);
            double yRight = YPos(node.Right!.MergeDistance);
            double yMerge = YPos(node.MergeDistance);

            // vertical connector from each child up to the merge height, then a horizontal bar
            g.DrawLine(linePen, (float)xLeft, (float)yLeft, (float)xLeft, (float)yMerge);
            g.DrawLine(linePen, (float)xRight, (float)yRight, (float)xRight, (float)yMerge);
            g.DrawLine(linePen, (float)xLeft, (float)yMerge, (float)xRight, (float)yMerge);

            using var distFont = new Font("Segoe UI", 7f, FontStyle.Italic);
            g.DrawString(node.MergeDistance.ToString("0.00"), distFont, Brushes.Gray, (float)xMid + 2, (float)yMerge - 12);
        }

        DrawNode(_root);
    }

    private static int CountLeaves(ClusterNode node) =>
        node.IsLeaf ? 1 : CountLeaves(node.Left!) + CountLeaves(node.Right!);
}
