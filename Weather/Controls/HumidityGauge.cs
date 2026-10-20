using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Weather.Controls;

public class HumidityGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<HumidityGauge, double>(nameof(Value), 58.0);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.FromArgb(60, 226, 232, 240));
    private static readonly IBrush ArcGradient = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#38BDF8"), 0.0),
            new GradientStop(Color.Parse("#3B82F6"), 0.5),
            new GradientStop(Color.Parse("#6366F1"), 1.0)
        }
    };
    private static readonly Pen TrackPen = new(TrackBrush, 8.0, lineCap: PenLineCap.Round);
    private static readonly Pen NeedlePen = new(new SolidColorBrush(Color.Parse("#1E293B")), 3.0, lineCap: PenLineCap.Round);
    private static readonly IBrush WhiteFill = new SolidColorBrush(Colors.White);
    private static readonly Pen PivotPen = new(new SolidColorBrush(Color.Parse("#3B82F6")), 2.5);
    private static readonly IBrush PivotCenter = new SolidColorBrush(Color.Parse("#1E293B"));

    static HumidityGauge()
    {
        AffectsRender<HumidityGauge>(ValueProperty);
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
            // 270 deg track: starts at 135 deg, ends at 405 deg (75.36 to 75.36)
            var trackGeom = new StreamGeometry();
            using (var ctx = trackGeom.Open())
            {
                ctx.BeginFigure(new Point(24.64, 75.36), false);
                ctx.ArcTo(new Point(75.36, 75.36), new Size(36, 36), 0, true, SweepDirection.Clockwise);
                ctx.EndFigure(false);
            }
            context.DrawGeometry(null, TrackPen, trackGeom);

            // Active Arc
            double clampedVal = Math.Clamp(Value, 0.0, 100.0);
            double sweepAngleDeg = (clampedVal / 100.0) * 270.0;

            if (sweepAngleDeg > 1.0)
            {
                double startAngleRad = 135.0 * Math.PI / 180.0;
                double endAngleRad = (135.0 + sweepAngleDeg) * Math.PI / 180.0;

                Point startPt = new(50 + 36 * Math.Cos(startAngleRad), 50 + 36 * Math.Sin(startAngleRad));
                Point endPt = new(50 + 36 * Math.Cos(endAngleRad), 50 + 36 * Math.Sin(endAngleRad));

                var activeGeom = new StreamGeometry();
                using (var ctx = activeGeom.Open())
                {
                    ctx.BeginFigure(startPt, false);
                    ctx.ArcTo(endPt, new Size(36, 36), 0, sweepAngleDeg > 180.0, SweepDirection.Clockwise);
                    ctx.EndFigure(false);
                }
                var activePen = new Pen(ArcGradient, 8.0, lineCap: PenLineCap.Round);
                context.DrawGeometry(null, activePen, activeGeom);
            }

            // Needle angle (-135 to +135 deg)
            double needleAngle = -135.0 + (clampedVal / 100.0) * 270.0;
            using (context.PushTransform(Matrix.CreateTranslation(-50, -50) * Matrix.CreateRotation(needleAngle * Math.PI / 180.0) * Matrix.CreateTranslation(50, 50)))
            {
                context.DrawLine(NeedlePen, new Point(50, 50), new Point(50, 22));
            }

            // Pivot pin
            context.DrawEllipse(WhiteFill, PivotPen, new Point(50, 50), 6, 6);
            context.DrawEllipse(PivotCenter, null, new Point(50, 50), 2.5, 2.5);
        }
    }
}
