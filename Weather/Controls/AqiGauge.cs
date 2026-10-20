using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Weather.Controls;

public class AqiGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<AqiGauge, double>(nameof(Value), 32.0);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.FromArgb(50, 226, 232, 240));
    private static readonly Pen TrackPen = new(TrackBrush, 7.0);

    static AqiGauge()
    {
        AffectsRender<AqiGauge>(ValueProperty);
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
            // Background track
            context.DrawEllipse(null, TrackPen, new Point(50, 50), 36, 36);

            // Determine active color
            Color activeColor = Value switch
            {
                <= 50 => Color.Parse("#10B981"),
                <= 100 => Color.Parse("#F59E0B"),
                <= 150 => Color.Parse("#F97316"),
                _ => Color.Parse("#EF4444")
            };
            var activeBrush = new SolidColorBrush(activeColor);
            var activePen = new Pen(activeBrush, 7.0, lineCap: PenLineCap.Round);

            // Active Arc (from -90 deg top clockwise)
            double clampedVal = Math.Clamp(Value, 0.0, 300.0);
            double sweepAngleDeg = Math.Max(2.0, (clampedVal / 200.0) * 360.0);
            if (sweepAngleDeg > 359.0) sweepAngleDeg = 359.0;

            double startAngleRad = -90.0 * Math.PI / 180.0;
            double endAngleRad = (-90.0 + sweepAngleDeg) * Math.PI / 180.0;

            Point startPt = new(50 + 36 * Math.Cos(startAngleRad), 50 + 36 * Math.Sin(startAngleRad));
            Point endPt = new(50 + 36 * Math.Cos(endAngleRad), 50 + 36 * Math.Sin(endAngleRad));

            var arcGeom = new StreamGeometry();
            using (var ctx = arcGeom.Open())
            {
                ctx.BeginFigure(startPt, false);
                ctx.ArcTo(endPt, new Size(36, 36), 0, sweepAngleDeg > 180.0, SweepDirection.Clockwise);
                ctx.EndFigure(false);
            }
            context.DrawGeometry(null, activePen, arcGeom);

            // Center stylized leaf icon
            var leafGeom = new StreamGeometry();
            using (var ctx = leafGeom.Open())
            {
                ctx.BeginFigure(new Point(57, 37), true);
                ctx.CubicBezierTo(new Point(45, 37), new Point(36, 46), new Point(36, 58));
                ctx.CubicBezierTo(new Point(48, 58), new Point(57, 49), new Point(57, 37));
                ctx.EndFigure(true);
            }
            context.DrawGeometry(activeBrush, null, leafGeom);

            // Leaf vein
            var veinPen = new Pen(new SolidColorBrush(Color.Parse("#A7F3D0")), 2.0, lineCap: PenLineCap.Round);
            var veinGeom = new StreamGeometry();
            using (var ctx = veinGeom.Open())
            {
                ctx.BeginFigure(new Point(36, 58), false);
                ctx.CubicBezierTo(new Point(40, 53), new Point(46, 47), new Point(57, 37));
                ctx.EndFigure(false);
            }
            context.DrawGeometry(null, veinPen, veinGeom);
        }
    }
}
