namespace DSFS.App.Controls;

/// <summary>
/// A minimal, dependency-free bar chart used to reproduce the "Evaluation Results" /
/// "Level of Accuracy" style graphs from Figures 4-6 of the thesis, drawn with plain GDI+
/// so the project needs no charting NuGet package.
/// </summary>
public class BarChartControl : Control
{
    public List<(string Label, double Value)> Data { get; set; } = new();

    /// <summary>Upper bound of the value axis (e.g. 1.0 for precision/recall/F-measure).</summary>
    public double MaxValue { get; set; } = 1.0;

    public string Title { get; set; } = string.Empty;

    public BarChartControl()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int marginLeft = 50, marginBottom = 40, marginTop = string.IsNullOrEmpty(Title) ? 20 : 40, marginRight = 20;
        int chartWidth = Width - marginLeft - marginRight;
        int chartHeight = Height - marginTop - marginBottom;

        if (chartWidth <= 0 || chartHeight <= 0) return;

        using var axisPen = new Pen(Color.DimGray, 1.5f);
        using var gridPen = new Pen(Color.Gainsboro, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        using var font = new Font("Segoe UI", 9f);
        using var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var barBrush = new SolidBrush(Color.FromArgb(0, 120, 212));
        using var textBrush = new SolidBrush(Color.Black);

        if (!string.IsNullOrEmpty(Title))
            g.DrawString(Title, titleFont, textBrush, new PointF(marginLeft, 8));

        // Axis
        g.DrawLine(axisPen, marginLeft, marginTop, marginLeft, marginTop + chartHeight);
        g.DrawLine(axisPen, marginLeft, marginTop + chartHeight, marginLeft + chartWidth, marginTop + chartHeight);

        // Gridlines + Y labels at 0%, 25%, 50%, 75%, 100% of MaxValue
        for (int i = 0; i <= 4; i++)
        {
            double fraction = i / 4.0;
            int y = marginTop + chartHeight - (int)(fraction * chartHeight);
            g.DrawLine(gridPen, marginLeft, y, marginLeft + chartWidth, y);
            string label = (fraction * MaxValue).ToString("P0");
            var size = g.MeasureString(label, font);
            g.DrawString(label, font, textBrush, marginLeft - size.Width - 4, y - size.Height / 2);
        }

        if (Data.Count == 0) return;

        int slot = chartWidth / Data.Count;
        int barWidth = Math.Max(10, (int)(slot * 0.5));

        for (int i = 0; i < Data.Count; i++)
        {
            var (label, value) = Data[i];
            double fraction = MaxValue > 0 ? Math.Clamp(value / MaxValue, 0, 1) : 0;
            int barHeight = (int)(fraction * chartHeight);
            int x = marginLeft + i * slot + (slot - barWidth) / 2;
            int y = marginTop + chartHeight - barHeight;

            g.FillRectangle(barBrush, x, y, barWidth, barHeight);
            g.DrawRectangle(axisPen, x, y, barWidth, barHeight);

            string valueText = value <= 1.0 ? value.ToString("P2") : value.ToString("0.##");
            var valueSize = g.MeasureString(valueText, font);
            g.DrawString(valueText, font, textBrush, x + barWidth / 2 - valueSize.Width / 2, y - valueSize.Height - 2);

            var labelSize = g.MeasureString(label, font);
            g.DrawString(label, font, textBrush,
                marginLeft + i * slot + slot / 2 - labelSize.Width / 2,
                marginTop + chartHeight + 6);
        }
    }
}
