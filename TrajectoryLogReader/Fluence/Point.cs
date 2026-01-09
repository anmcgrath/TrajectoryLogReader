namespace TrajectoryLogReader.Fluence;

/// <summary>
/// Represents a 2D point with double precision.
/// </summary>
public struct Point
{
    /// <summary>
    /// The X coordinate.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// The Y coordinate.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Point"/> struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"({X}, {Y})";
}