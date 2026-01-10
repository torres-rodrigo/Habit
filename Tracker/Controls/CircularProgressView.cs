namespace Tracker.Controls;

public class CircularProgressView : GraphicsView
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(CircularProgressView), 0.0, propertyChanged: OnProgressChanged);

    public static readonly BindableProperty ProgressColorProperty =
        BindableProperty.Create(nameof(ProgressColor), typeof(Color), typeof(CircularProgressView), Colors.Green, propertyChanged: OnProgressChanged);

    public static readonly BindableProperty TrackColorProperty =
        BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(CircularProgressView), Colors.LightGray, propertyChanged: OnProgressChanged);

    public static readonly BindableProperty StrokeWidthProperty =
        BindableProperty.Create(nameof(StrokeWidth), typeof(float), typeof(CircularProgressView), 3f, propertyChanged: OnProgressChanged);

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Color ProgressColor
    {
        get => (Color)GetValue(ProgressColorProperty);
        set => SetValue(ProgressColorProperty, value);
    }

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public float StrokeWidth
    {
        get => (float)GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public CircularProgressView()
    {
        Drawable = new CircularProgressDrawable(this);
    }

    private static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CircularProgressView view)
        {
            view.Invalidate();
        }
    }

    private class CircularProgressDrawable : IDrawable
    {
        private readonly CircularProgressView _view;

        public CircularProgressDrawable(CircularProgressView view)
        {
            _view = view;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var centerX = dirtyRect.Width / 2;
            var centerY = dirtyRect.Height / 2;
            var radius = Math.Min(centerX, centerY) - _view.StrokeWidth / 2;

            // Clamp progress between 0 and 1
            var clampedProgress = Math.Max(0, Math.Min(_view.Progress, 1.0));

            // Draw background circle (remaining progress) - always draw full circle
            canvas.StrokeColor = _view.TrackColor;
            canvas.StrokeSize = _view.StrokeWidth;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawCircle(centerX, centerY, radius);

            // Draw progress arc (completed progress) on top
            if (clampedProgress > 0)
            {
                canvas.StrokeColor = _view.ProgressColor;
                canvas.StrokeSize = _view.StrokeWidth;
                canvas.StrokeLineCap = LineCap.Round;

                // For 100%, draw a complete circle instead of an arc to avoid gaps
                if (clampedProgress >= 0.9999)
                {
                    canvas.DrawCircle(centerX, centerY, radius);
                }
                else
                {
                    // Use PathF to draw the arc correctly
                    var path = new PathF();
                    
                    // Calculate the angle in radians (starting from top, going clockwise)
                    var startAngleRad = -Math.PI / 2; // -90 degrees (top of circle)
                    var sweepAngleRad = clampedProgress * 2 * Math.PI; // Convert progress to radians
                    
                    // Calculate start point
                    var startX = centerX + radius * (float)Math.Cos(startAngleRad);
                    var startY = centerY + radius * (float)Math.Sin(startAngleRad);
                    
                    // Move to start point
                    path.MoveTo(startX, startY);
                    
                    // Add arc using multiple line segments for accuracy
                    var segments = Math.Max(1, (int)(clampedProgress * 100)); // More segments for smoother arc
                    for (int i = 1; i <= segments; i++)
                    {
                        var angle = startAngleRad + (sweepAngleRad * i / segments);
                        var x = centerX + radius * (float)Math.Cos(angle);
                        var y = centerY + radius * (float)Math.Sin(angle);
                        path.LineTo(x, y);
                    }
                    
                    canvas.DrawPath(path);
                }
            }
        }
    }
}
