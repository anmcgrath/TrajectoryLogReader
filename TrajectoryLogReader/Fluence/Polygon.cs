namespace TrajectoryLogReader.Fluence;

internal class Polygon
{
    public IReadOnlyCollection<Point> Vertices => _vertices;
    private readonly List<Point> _vertices;

    public Polygon(List<Point> vertices)
    {
        _vertices = vertices;
    }

    /// <summary>
    /// Calculates the area of the polygon using the Shoelace formula.
    /// Returns the absolute area regardless of vertex winding order.
    /// </summary>
    /// <returns>The area of the polygon</returns>
    public double Area()
    {
        if (_vertices.Count < 3)
            return 0;

        double sum = 0;

        for (int i = 0; i < _vertices.Count; i++)
        {
            var current = _vertices[i];
            var next = _vertices[(i + 1) % _vertices.Count];

            sum += current.X * next.Y - next.X * current.Y;
        }

        return Math.Abs(sum) / 2.0;
    }
}