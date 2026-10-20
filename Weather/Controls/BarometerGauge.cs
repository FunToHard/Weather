using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Weather.Controls;

public class BarometerGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<BarometerGauge, double>(nameof(Value), 1013.0);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.FromArgb(60, 226, 232, 240));
    private static readonly IBrush BarometerGradient = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#6366F1"), 0.0), // Low (970 hPa) - Purple/Indigo
            new GradientStop(Color.Parse("#06B6D4"), 0.5), // Normal (1013 hPa) - Cyan/Teal
            new GradientStop(Color.Parse("#F59E0B"), 1.0)  // High (1050 hPa) - Amber
        }
    };
    private static readonly Pen TrackPen = new(TrackBrush, 7.0, lineCap: PenLineCap.Round);
    private static readonly Pen NeedlePen = new(new SolidColorBrush(Color.Parse("#1E293B")), 2.5, lineCap: PenLineCap.Round);
    private static readonly IBrush WhiteFill = new SolidColorBrush(Colors.White);
    private static readonly Pen PivotPen = new(new SolidColorBrush(Color.Parse("#06B6D4")), 2.0);
    private static readonly IBrush PivotCenter = new SolidColorBrush(Color.Parse("#1E293B"));

    static BarometerGauge()
    {
        AffectsRender<BarometerGauge>(ValueProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0) return;

        double scale = size / 100.0;
        var center = new Point(Bounds.Width / 2.0, Bounds.Height / 2.0);

        using (context.PushTransform(Matrix.CreateTranslation(center.X - 50 * scale, center.Y - 50 * scale) * Matrix.CreateScale(scale, scale)))
        {
            // 200° Arc track from 170° to 370° (starts bottom-left, arches up through top, down to bottom-right)
            // Center is (50, 52), radius is 34
            double cx = 50.0;
            double cy = 52.0;
            double r = 34.0;

            double startAngleDeg = 170.0;
            double endAngleDeg = 370.0;
            double totalSpan = 200.0;

            double startRad = startAngleDeg * Math.PI / 180.0;
            double endRad = endAngleDeg * Math.PI / 180.0;

            Point startPt = new(cx + r * Math.Cos(startRad), cy + r * Math.Sin(startRad));
            Point endPt = new(cx + r * Math.Cos(endRad), cy + r * Math.Sin(endRad));

            var trackGeom = new StreamGeometry();
            using (var ctx = trackGeom.Open())
            {
                ctx.BeginFigure(startPt, false);
                ctx.ArcTo(endPt, new Size(r, r), 0, true, SweepDirection.Clockwise);
                ctx.EndFigure(false);
            }
            context.DrawGeometry(null, TrackPen, trackGeom);

            // Active Arc
            double clampedVal = Math.Clamp(Value, 970.0, 1050.0);
            double ratio = (clampedVal - 970.0) / 80.0;
            double sweepDeg = ratio * totalSpan;

            if (sweepDeg > 1.0)
            {
                double curRad = (startAngleDeg + sweepDeg) * Math.PI / 180.0;
                Point curPt = new(cx + r * Math.Cos(curRad), cy + r * Math.Sin(curRad));

                var activeGeom = new StreamGeometry();
                using (var ctx = activeGeom.Open())
                {
                    ctx.BeginFigure(startPt, false);
                    ctx.ArcTo(curPt, new Size(r, r), 0, sweepDeg > 180.0, SweepDirection.Clockwise);
                    ctx.EndFigure(false);
                }
                var activePen = new Pen(BarometerGradient, 7.0, lineCap: PenLineCap.Round);
                context.DrawGeometry(null, activePen, activeGeom);
            }

            // Needle angle (relative to 12 o'clock, which is -90° in standard math or 0° straight up)
            // At startAngle 170° (bottom left), relative to vertical is -100°
            // At 270° (top center, 1010 hPa), angle is 0°
            // Needle rotates from -100° to +100°
            double needleAngle = -100.0 + (ratio * 200.0);
            using (context.PushTransform(Matrix.CreateTranslation(-cx, -cy) * Matrix.CreateRotation(needleAngle * Math.PI / 180.0) * Matrix.CreateTranslation(cx, cy)))
            {
                context.DrawLine(NeedlePen, new Point(cx, cy), new Point(cx, cy - 25));
            }

            // Center pivot
            context.DrawEllipse(WhiteFill, PivotPen, new Point(cx, cy), 5.5, 5.5);
            context.DrawEllipse(PivotCenter, null, new Point(cx, cy), 2.0, 2.0);
        }
    }
}
