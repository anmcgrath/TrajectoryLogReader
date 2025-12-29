namespace TrajectoryLogReader.Fluence;

internal class Intersection
{
    public static Polygon Intersect(Polygon polygon, Rect rect)
    {
        // Define the four edges of the rectangle
        var left = rect.X;
        var right = rect.X + rect.Width;
        var bottom = rect.Y;
        var top = rect.Y + rect.Height;

        // Start with the polygon's vertices
        var vertices = new List<Point>(polygon.Vertices);

        // Clip against each edge of the rectangle sequentially
        vertices = ClipAgainstEdge(vertices, left, EdgeType.Left);
        vertices = ClipAgainstEdge(vertices, right, EdgeType.Right);
        vertices = ClipAgainstEdge(vertices, bottom, EdgeType.Bottom);
        vertices = ClipAgainstEdge(vertices, top, EdgeType.Top);

        return new Polygon(vertices);
    }

    private enum EdgeType
    {
        Left,
        Right,
        Bottom,
        Top
    }

    private static List<Point> ClipAgainstEdge(List<Point> vertices, double edgePosition, EdgeType edgeType)
    {
        if (vertices.Count == 0) return vertices;

        var outputVertices = new List<Point>();

        for (int i = 0; i < vertices.Count; i++)
        {
            var current = vertices[i];
            var next = vertices[(i + 1) % vertices.Count];

            bool currentInside = IsInside(current, edgePosition, edgeType);
            bool nextInside = IsInside(next, edgePosition, edgeType);

            if (currentInside && nextInside)
            {
                // Both inside: add next vertex
                outputVertices.Add(next);
            }
            else if (currentInside && !nextInside)
            {
                // Leaving: add intersection point
                outputVertices.Add(GetIntersection(current, next, edgePosition, edgeType));
            }
            else if (!currentInside && nextInside)
            {
                // Entering: add intersection point and next vertex
                outputVertices.Add(GetIntersection(current, next, edgePosition, edgeType));
                outputVertices.Add(next);
            }
            // else: both outside, add nothing
        }

        return outputVertices;
    }

    private static bool IsInside(Point point, double edgePosition, EdgeType edgeType)
    {
        switch (edgeType)
        {
            case EdgeType.Left:
                return point.X >= edgePosition;
            case EdgeType.Right:
                return point.X <= edgePosition;
            case EdgeType.Bottom:
                return point.Y >= edgePosition;
            case EdgeType.Top:
                return point.Y <= edgePosition;
            default:
                return false;
        }
    }

    private static Point GetIntersection(Point p1, Point p2, double edgePosition, EdgeType edgeType)
    {
        switch (edgeType)
        {
            case EdgeType.Left:
            case EdgeType.Right:
                return new Point
                    { X = edgePosition, Y = p1.Y + (p2.Y - p1.Y) * (edgePosition - p1.X) / (p2.X - p1.X) };
            case EdgeType.Bottom:
            case EdgeType.Top:
                return new Point
                    { X = p1.X + (p2.X - p1.X) * (edgePosition - p1.Y) / (p2.Y - p1.Y), Y = edgePosition };
            default:
                return p1;
        }
    }
}