using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Weather.Controls;

public class CompassGauge : Control
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<CompassGauge, double>(nameof(Angle), 315.0);

    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    private static readonly Typeface CardinalTypeface = new(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
    private static readonly Pen DialTrackPen = new(new SolidColorBrush(Color.Parse("#94A3B8")), 1.5, dashStyle: new DashStyle(new double[] { 2, 4 }, 0));
    private static readonly Pen InnerTrackPen = new(new SolidColorBrush(Color.FromArgb(100, 203, 213, 225)), 1.0);
    private static readonly IBrush NorthBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#3B82F6"), 0.0),
            new GradientStop(Color.Parse("#1D4ED8"), 1.0)
        }
    };
    private static readonly IBrush SouthBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#93C5FD"), 0.0),
            new GradientStop(Color.Parse("#60A5FA"), 1.0)
        }
    };
    private static readonly IBrush WhiteFill = new SolidColorBrush(Colors.White);
    private static readonly Pen PivotPen = new(new SolidColorBrush(Color.Parse("#3B82F6")), 2.0);
    private static readonly IBrush PivotCenterBrush = new SolidColorBrush(Color.Parse("#1D4ED8"));

    static CompassGauge()
    {
        AffectsRender<CompassGauge>(AngleProperty);
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
            // Dial rings
            context.DrawEllipse(null, DialTrackPen, new Point(50, 50), 44, 44);
            context.DrawEllipse(null, InnerTrackPen, new Point(50, 50), 38, 38);

            // Cardinal labels
            DrawText(context, "N", new Point(50, 7), Color.Parse("#3B82F6"), 11);
            DrawText(context, "S", new Point(50, 83), Color.Parse("#64748B"), 9);
            DrawText(context, "E", new Point(88, 44), Color.Parse("#64748B"), 9);
            DrawText(context, "W", new Point(12, 44), Color.Parse("#64748B"), 9);

            // Needle rotated by Angle
            using (context.PushTransform(Matrix.CreateTranslation(-50, -50) * Matrix.CreateRotation(Angle * Math.PI / 180.0) * Matrix.CreateTranslation(50, 50)))
            {
                // North arrow
                var northGeometry = new StreamGeometry();
                using (var ctx = northGeometry.Open())
                {
                    ctx.BeginFigure(new Point(50, 16), true);
                    ctx.LineTo(new Point(45, 50));
                    ctx.LineTo(new Point(50, 46));
                    ctx.LineTo(new Point(55, 50));
                    ctx.EndFigure(true);
                }
                context.DrawGeometry(NorthBrush, null, northGeometry);

                // South arrow
                var southGeometry = new StreamGeometry();
                using (var ctx = southGeometry.Open())
                {
                    ctx.BeginFigure(new Point(50, 84), true);
                    ctx.LineTo(new Point(45, 50));
                    ctx.LineTo(new Point(50, 54));
                    ctx.LineTo(new Point(55, 50));
                    ctx.EndFigure(true);
                }
                context.DrawGeometry(SouthBrush, null, southGeometry);
            }

            // Pivot pin
            context.DrawEllipse(WhiteFill, PivotPen, new Point(50, 50), 5.5, 5.5);
            context.DrawEllipse(PivotCenterBrush, null, new Point(50, 50), 2.5, 2.5);
        }
    }

    private static void DrawText(DrawingContext context, string text, Point point, Color color, double fontSize)
    {
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            CardinalTypeface,
            fontSize,
            new SolidColorBrush(color));
        context.DrawText(formatted, new Point(point.X - formatted.Width / 2.0, point.Y));
    }
}
