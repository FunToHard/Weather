using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Weather.Controls;

public class VisibilityGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<VisibilityGauge, double>(nameof(Value), 10.0);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.FromArgb(60, 226, 232, 240));
    private static readonly IBrush RingBrush = new SolidColorBrush(Color.FromArgb(40, 14, 165, 233));
    private static readonly IBrush ActiveBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#38BDF8"), 0.0),
            new GradientStop(Color.Parse("#0EA5E9"), 0.5),
            new GradientStop(Color.Parse("#0284C7"), 1.0)
        }
    };
    private static readonly Pen TrackPen = new(TrackBrush, 7.0, lineCap: PenLineCap.Round);
    private static readonly Pen ActivePen = new(ActiveBrush, 7.0, lineCap: PenLineCap.Round);
    private static readonly Pen RingPen = new(RingBrush, 1.5, dashStyle: DashStyle.Dash);
    private static readonly IBrush EyeGlow = new RadialGradientBrush
    {
        GradientStops =
        {
            new GradientStop(Color.FromArgb(180, 14, 165, 233), 0.0),
            new GradientStop(Color.FromArgb(60, 56, 189, 248), 0.6),
            new GradientStop(Color.FromArgb(0, 56, 189, 248), 1.0)
        }
    };
    private static readonly IBrush EyeIris = new SolidColorBrush(Color.Parse("#0284C7"));
    private static readonly IBrush EyePupil = new SolidColorBrush(Color.Parse("#FFFFFF"));

    private static readonly StreamGeometry Ring1Geom = CreateArc(50.0, 52.0, 24.0);
    private static readonly StreamGeometry Ring2Geom = CreateArc(50.0, 52.0, 15.0);

    static VisibilityGauge()
    {
        AffectsRender<VisibilityGauge>(ValueProperty);
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
            double cx = 50.0;
            double cy = 52.0;
            double r = 34.0;

            // 180° Arch (from 180° to 360°)
            var trackGeom = new StreamGeometry();
            using (var ctx = trackGeom.Open())
            {
                ctx.BeginFigure(new Point(cx - r, cy), false);
                ctx.ArcTo(new Point(cx + r, cy), new Size(r, r), 0, false, SweepDirection.Clockwise);
                ctx.EndFigure(false);
            }
            context.DrawGeometry(null, TrackPen, trackGeom);

            // Pre-cached static range rings (zero allocations during draw)
            context.DrawGeometry(null, RingPen, Ring1Geom);
            context.DrawGeometry(null, RingPen, Ring2Geom);

            // Active Arc based on Value (0 to 16+ miles)
            double clampedVal = Math.Clamp(Value, 0.0, 16.0);
            double ratio = clampedVal / 16.0;
            double sweepDeg = ratio * 180.0;

            if (sweepDeg > 1.0)
            {
                double curRad = (180.0 + sweepDeg) * Math.PI / 180.0;
                Point curPt = new(cx + r * Math.Cos(curRad), cy + r * Math.Sin(curRad));

                var activeGeom = new StreamGeometry();
                using (var ctx = activeGeom.Open())
                {
                    ctx.BeginFigure(new Point(cx - r, cy), false);
                    ctx.ArcTo(curPt, new Size(r, r), 0, sweepDeg > 180.0, SweepDirection.Clockwise);
                    ctx.EndFigure(false);
                }
                context.DrawGeometry(null, ActivePen, activeGeom);
            }

            // Central Horizon Eye / Beacon Indicator
            context.DrawEllipse(EyeGlow, null, new Point(cx, cy), 16.0, 16.0);
            context.DrawEllipse(EyeIris, null, new Point(cx, cy), 7.0, 7.0);
            context.DrawEllipse(EyePupil, null, new Point(cx, cy), 2.5, 2.5);
        }
    }

    private static StreamGeometry CreateArc(double cx, double cy, double r)
    {
        var geom = new StreamGeometry();
        using var ctx = geom.Open();
        ctx.BeginFigure(new Point(cx - r, cy), false);
        ctx.ArcTo(new Point(cx + r, cy), new Size(r, r), 0, false, SweepDirection.Clockwise);
        ctx.EndFigure(false);
        return geom;
    }
}
