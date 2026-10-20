using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Weather.Controls;

public class TemperatureTrendGraph : Control
{
    private static readonly IBrush LineBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#F59E0B"), 0.0),
            new GradientStop(Color.Parse("#818CF8"), 0.5),
            new GradientStop(Color.Parse("#3B82F6"), 1.0)
        }
    };

    private static readonly IBrush AreaBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.FromArgb(130, 245, 158, 11), 0.0),
            new GradientStop(Color.FromArgb(70, 129, 140, 248), 0.5),
            new GradientStop(Color.FromArgb(10, 59, 130, 246), 1.0)
        }
    };

    private static readonly Pen LinePen = new(LineBrush, 3.5, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
    private static readonly IBrush WhiteFill = new SolidColorBrush(Colors.White);

    public static readonly StyledProperty<IReadOnlyList<double>?> TemperaturesProperty =
        AvaloniaProperty.Register<TemperatureTrendGraph, IReadOnlyList<double>?>(nameof(Temperatures));

    public IReadOnlyList<double>? Temperatures
    {
        get => GetValue(TemperaturesProperty);
        set => SetValue(TemperaturesProperty, value);
    }

    static TemperatureTrendGraph()
    {
        AffectsRender<TemperatureTrendGraph>(TemperaturesProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var temps = Temperatures;
        if (temps == null || temps.Count < 2 || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        int count = temps.Count;
        double minTemp = double.MaxValue;
        double maxTemp = double.MinValue;
        foreach (var t in temps)
        {
            if (t < minTemp) minTemp = t;
            if (t > maxTemp) maxTemp = t;
        }

        double tempRange = maxTemp - minTemp;
        if (tempRange < 1.0) tempRange = 1.0;

        double topPadding = 10.0;
        double bottomPadding = 10.0;
        double availableHeight = Bounds.Height - topPadding - bottomPadding;
        if (availableHeight <= 0) return;

        var points = new Point[count];
        for (int i = 0; i < count; i++)
        {
            double x = (i + 0.5) * (Bounds.Width / count);
            double normalized = (maxTemp - temps[i]) / tempRange;
            double y = topPadding + normalized * availableHeight;
            points[i] = new Point(x, y);
        }

        var streamGeometry = new StreamGeometry();
        using (var ctx = streamGeometry.Open())
        {
            ctx.BeginFigure(points[0], false);
            for (int i = 0; i < count - 1; i++)
            {
                Point p0 = i > 0 ? points[i - 1] : points[i];
                Point p1 = points[i];
                Point p2 = points[i + 1];
                Point p3 = (i + 2 < count) ? points[i + 2] : p2;

                Point cp1 = new Point(
                    p1.X + (p2.X - p0.X) / 6.0,
                    p1.Y + (p2.Y - p0.Y) / 6.0);
                Point cp2 = new Point(
                    p2.X + (p3.X - p1.X) / 6.0,
                    p2.Y + (p3.Y - p1.Y) / 6.0);

                ctx.CubicBezierTo(cp1, cp2, p2);
            }
            ctx.EndFigure(false);
        }

        var areaGeometry = new StreamGeometry();
        using (var ctx = areaGeometry.Open())
        {
            ctx.BeginFigure(new Point(points[0].X, Bounds.Height), true);
            ctx.LineTo(points[0]);
            for (int i = 0; i < count - 1; i++)
            {
                Point p0 = i > 0 ? points[i - 1] : points[i];
                Point p1 = points[i];
                Point p2 = points[i + 1];
                Point p3 = (i + 2 < count) ? points[i + 2] : p2;

                Point cp1 = new Point(
                    p1.X + (p2.X - p0.X) / 6.0,
                    p1.Y + (p2.Y - p0.Y) / 6.0);
                Point cp2 = new Point(
                    p2.X + (p3.X - p1.X) / 6.0,
                    p2.Y + (p3.Y - p1.Y) / 6.0);

                ctx.CubicBezierTo(cp1, cp2, p2);
            }
            ctx.LineTo(new Point(points[count - 1].X, Bounds.Height));
            ctx.EndFigure(true);
        }

        context.DrawGeometry(AreaBrush, null, areaGeometry);
        context.DrawGeometry(null, LinePen, streamGeometry);

        for (int i = 0; i < count; i++)
        {
            Color nodeColor = i == 0 ? Color.Parse("#F59E0B") : (i == count - 1 ? Color.Parse("#3B82F6") : Color.Parse("#818CF8"));
            var nodePen = new Pen(new SolidColorBrush(nodeColor), 2.5);
            context.DrawEllipse(WhiteFill, nodePen, points[i], 4.5, 4.5);
        }
    }
}
