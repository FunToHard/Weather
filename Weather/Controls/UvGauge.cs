using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Weather.Controls;

public class UvGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<UvGauge, double>(nameof(Value), 3.0);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static readonly IBrush RainbowBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#10B981"), 0.0),
            new GradientStop(Color.Parse("#FBBF24"), 0.35),
            new GradientStop(Color.Parse("#F97316"), 0.65),
            new GradientStop(Color.Parse("#EF4444"), 1.0)
        }
    };
    private static readonly Pen RainbowPen = new(RainbowBrush, 8.0, lineCap: PenLineCap.Round);
    private static readonly Pen NeedlePen = new(new SolidColorBrush(Color.Parse("#1E293B")), 3.0, lineCap: PenLineCap.Round);
    private static readonly IBrush WhiteFill = new SolidColorBrush(Colors.White);
    private static readonly Pen PivotPen = new(new SolidColorBrush(Color.Parse("#F59E0B")), 2.5);
    private static readonly IBrush PivotCenter = new SolidColorBrush(Color.Parse("#1E293B"));

    static UvGauge()
    {
        AffectsRender<UvGauge>(ValueProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        double scale = Math.Min(width / 100.0, height / 70.0);
        var center = new Point(width / 2.0, height / 2.0 + 5.0 * scale);

        using (context.PushTransform(Matrix.CreateTranslation(center.X - 50 * scale, center.Y - 56 * scale) * Matrix.CreateScale(scale, scale)))
        {
            // Rainbow semicircle arc (from 14, 56 to 86, 56)
            var arcGeom = new StreamGeometry();
            using (var ctx = arcGeom.Open())
            {
                ctx.BeginFigure(new Point(14, 56), false);
                ctx.ArcTo(new Point(86, 56), new Size(36, 36), 0, false, SweepDirection.Clockwise);
                ctx.EndFigure(false);
            }
            context.DrawGeometry(null, RainbowPen, arcGeom);

            // Needle angle (-90 deg to +90 deg based on Value clamped 0 to 11)
            double clampedVal = Math.Clamp(Value, 0.0, 11.0);
            double needleAngle = -90.0 + (clampedVal / 11.0) * 180.0;

            using (context.PushTransform(Matrix.CreateTranslation(-50, -56) * Matrix.CreateRotation(needleAngle * Math.PI / 180.0) * Matrix.CreateTranslation(50, 56)))
            {
                context.DrawLine(NeedlePen, new Point(50, 56), new Point(50, 26));
            }

            // Pivot pin
            context.DrawEllipse(WhiteFill, PivotPen, new Point(50, 56), 6, 6);
            context.DrawEllipse(PivotCenter, null, new Point(50, 56), 2.5, 2.5);
        }
    }
}
