using System.Runtime.CompilerServices;
[assembly:InternalsVisibleTo("TrajectoryLogReader.Tests")]

namespace TrajectoryLogReader.Fluence;

internal class Rect
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Rect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Contains(double x, double y)
    {
        return x >= X && x <= X + Width && y >= Y && y <= Y + Height;
    }

    public Rect()
    {
    }

    public Point BottomLeft() => new Point(X, Y);
    public Point BottomRight() => new Point(X + Width, Y);
    public Point TopLeft() => new Point(X, Y + Height);
    public Point TopRight() => new Point(X + Width, Y + Height);

    public double Area => Width * Height;
}