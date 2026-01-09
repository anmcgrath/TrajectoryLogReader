using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TrajectoryLogReader.Tests")]

namespace TrajectoryLogReader.Fluence;

/// <summary>
/// Represents a rectangle defined by its bottom-left corner, width, and height.
/// </summary>
public class Rect
{
    /// <summary>
    /// X coordinate of the bottom-left corner.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Y coordinate of the bottom-left corner.
    /// </summary>
    public double Y { get; set; }
    /// <summary>
    /// Width of the rectangle.
    /// </summary>
    public double Width { get; set; }
    /// <summary>
    /// Height of the rectangle.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rect"/> class.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    public Rect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Determines if the specified point is contained within the rectangle.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>True if the point is inside, otherwise false.</returns>
    public bool Contains(double x, double y)
    {
        return x >= X && x <= X + Width && y >= Y && y <= Y + Height;
    }

    public Rect()
    {
    }

    /// <summary>
    /// Gets the bottom-left point.
    /// </summary>
    public Point BottomLeft() => new Point(X, Y);
    /// <summary>
    /// Gets the bottom-right point.
    /// </summary>
    public Point BottomRight() => new Point(X + Width, Y);
    /// <summary>
    /// Gets the top-left point.
    /// </summary>
    public Point TopLeft() => new Point(X, Y + Height);
    /// <summary>
    /// Gets the top-right point.
    /// </summary>
    public Point TopRight() => new Point(X + Width, Y + Height);

    /// <summary>
    /// The area of the rectangle.
    /// </summary>
    public double Area => Width * Height;
}