using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace IPConfig.Controls;

/// <summary>
/// <see href="https://stackoverflow.com/a/42342862/4380178">WPF reactangle border with corner from connecting line of two dashes</see>
/// </summary>
public class AlignDashCornerRect : FrameworkElement
{
    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(
            "Fill",
            typeof(Brush),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(default(Brush), OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeDashCapProperty =
        DependencyProperty.Register(
            "StrokeDashCap",
            typeof(PenLineCap),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(PenLineCap.Flat, OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeDashLineProperty =
        DependencyProperty.Register(
            "StrokeDashLine",
            typeof(double),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(default(double), OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeDashSpaceProperty =
        DependencyProperty.Register(
            "StrokeDashSpace",
            typeof(double),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(default(double), OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeEndLineCapProperty =
        DependencyProperty.Register(
            "StrokeEndLineCap",
            typeof(PenLineCap),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(PenLineCap.Flat, OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeLineJoinProperty =
        DependencyProperty.Register(
            "StrokeLineJoin",
            typeof(PenLineJoin),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(PenLineJoin.Miter, OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeMiterLimitProperty =
        DependencyProperty.Register(
            "StrokeMiterLimit",
            typeof(double),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(10.0d, OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(
            "Stroke",
            typeof(Brush),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(default(Brush), OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeStartLineCapProperty =
        DependencyProperty.Register(
            "StrokeStartLineCap",
            typeof(PenLineCap),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(PenLineCap.Flat, OnVisualPropertyChanged));

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(
            "StrokeThickness",
            typeof(double),
            typeof(AlignDashCornerRect),
            new PropertyMetadata(default(double), OnVisualPropertyChanged));

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public PenLineCap StrokeDashCap
    {
        get => (PenLineCap)GetValue(StrokeDashCapProperty);
        set => SetValue(StrokeDashCapProperty, value);
    }

    public double StrokeDashLine
    {
        get => (double)GetValue(StrokeDashLineProperty);
        set => SetValue(StrokeDashLineProperty, value);
    }

    public double StrokeDashSpace
    {
        get => (double)GetValue(StrokeDashSpaceProperty);
        set => SetValue(StrokeDashSpaceProperty, value);
    }

    public PenLineCap StrokeEndLineCap
    {
        get => (PenLineCap)GetValue(StrokeEndLineCapProperty);
        set => SetValue(StrokeEndLineCapProperty, value);
    }

    public PenLineJoin StrokeLineJoin
    {
        get => (PenLineJoin)GetValue(StrokeLineJoinProperty);
        set => SetValue(StrokeLineJoinProperty, value);
    }

    public double StrokeMiterLimit
    {
        get => (double)GetValue(StrokeMiterLimitProperty);
        set => SetValue(StrokeMiterLimitProperty, value);
    }

    public PenLineCap StrokeStartLineCap
    {
        get => (PenLineCap)GetValue(StrokeStartLineCapProperty);
        set => SetValue(StrokeStartLineCapProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public AlignDashCornerRect()
    {
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        double x = StrokeThickness / 2.0;

        var horizontalPen = GetPen(ActualWidth - (2.0 * x));
        var verticalPen = GetPen(ActualHeight - (2.0 * x));

        drawingContext.DrawRectangle(Fill, null, new Rect(new Point(0, 0), new Size(w, h)));

        drawingContext.DrawLine(horizontalPen, new Point(x, x), new Point(w - x, x));
        drawingContext.DrawLine(horizontalPen, new Point(x, h - x), new Point(w - x, h - x));

        drawingContext.DrawLine(verticalPen, new Point(x, x), new Point(x, h - x));
        drawingContext.DrawLine(verticalPen, new Point(w - x, x), new Point(w - x, h - x));
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((AlignDashCornerRect)d).InvalidateVisual();
    }

    private IEnumerable<double> GetDashArray(double length)
    {
        double useableLength = length - StrokeDashLine;
        int lines = (int)Math.Round(useableLength / (StrokeDashLine + StrokeDashSpace));
        useableLength -= lines * StrokeDashLine;
        double actualSpacing = useableLength / lines;

        yield return StrokeDashLine / StrokeThickness;
        yield return actualSpacing / StrokeThickness;
    }

    private Pen GetPen(double length)
    {
        var dashArray = GetDashArray(length);

        return new Pen(Stroke, StrokeThickness) {
            DashStyle = new DashStyle(dashArray, 0),
            DashCap = StrokeDashCap,
            StartLineCap = StrokeStartLineCap,
            EndLineCap = StrokeEndLineCap,
            LineJoin = StrokeLineJoin,
            MiterLimit = StrokeMiterLimit
        };
    }
}
