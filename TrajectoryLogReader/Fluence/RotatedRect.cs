namespace TrajectoryLogReader.Fluence;

internal class RotatedRect
{
    public Rect Bounds { get; }
    public Polygon Polygon { get; }

    /// <summary>
    /// Inverse rotation matrix
    /// </summary>
    private readonly Matrix _invMatrix;

    /// <summary>
    /// The angle the rectangle was rotated by.
    /// </summary>
    public double Angle { get; }

    /// <summary>
    /// Original rect
    /// </summary>
    private readonly Rect _originalRect;

    private RotatedRect(List<Point> coords, double angle, Rect originalRect)
    {
        var x0 = coords.Min(p => p.X);
        var xMax = coords.Max(p => p.X);
        var y0 = coords.Min(p => p.Y);
        var yMax = coords.Max(p => p.Y);

        Bounds = new Rect()
        {
            X = x0,
            Y = y0,
            Width = xMax - x0,
            Height = yMax - y0,
        };

        _invMatrix = Matrix.Rotation(-angle);
        Angle = angle;
        _originalRect = originalRect;
        Polygon = new Polygon(coords);
    }

    public bool Contains(Point p)
    {
        // rotate point by inverse of rotation matrix
        // Use the pre-calculated inverse matrix
        var rotatedP = _invMatrix * p;
        return _originalRect.Contains(rotatedP.X, rotatedP.Y);
    }

    public bool Contains(Rect rect)
    {
        return Contains(rect.TopLeft()) &&
               Contains(rect.BottomRight()) &&
               Contains(rect.TopRight()) &&
               Contains(rect.BottomLeft());
    }

    /// <summary>
    /// Creates a rotated rectangle
    /// </summary>
    /// <param name="rect">The rectangle to rotate around (0, 0)</param>
    /// <param name="angle">The rotation angle in radians</param>
    /// <returns></returns>
    public static RotatedRect Create(Rect rect, double angle)
    {
        var matrix = Matrix.Rotation(angle);

        var newCoords = new List<Point>()
        {
            matrix * rect.BottomLeft(),
            matrix * rect.TopLeft(),
            matrix * rect.TopRight(),
            matrix * rect.BottomRight(),
        };

        return new RotatedRect(newCoords, angle, rect);
    }
}
