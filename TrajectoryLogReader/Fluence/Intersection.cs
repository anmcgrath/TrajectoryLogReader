namespace TrajectoryLogReader.Fluence;

internal static class Intersection
{
    /// <summary>
    /// Calculates the area of the intersection between a polygon and a rectangle.
    /// Optimized to avoid heap allocations using Span and stackalloc.
    /// </summary>
    public static double GetIntersectionArea(List<Point> polygonVertices, Rect rect)
    {
        // Max vertices for a rect-rect intersection is usually small (max 8).
        // We use a buffer of 16 to be safe.
        Span<Point> buffer1 = stackalloc Point[16];
        Span<Point> buffer2 = stackalloc Point[16];

        // Copy initial vertices to buffer1
        int count = polygonVertices.Count;
        if (count > 16) throw new InvalidOperationException("Polygon has too many vertices for stack optimization");
        
        // Manual copy from List to Span
        for (int i = 0; i < count; i++)
        {
            buffer1[i] = polygonVertices[i];
        }

        // Clip against edges
        // We swap buffers: input -> output
        
        // Left
        count = ClipAgainstEdge(buffer1.Slice(0, count), buffer2, rect.X, EdgeType.Left);
        if (count == 0) return 0;

        // Right
        count = ClipAgainstEdge(buffer2.Slice(0, count), buffer1, rect.X + rect.Width, EdgeType.Right);
        if (count == 0) return 0;

        // Bottom
        count = ClipAgainstEdge(buffer1.Slice(0, count), buffer2, rect.Y, EdgeType.Bottom);
        if (count == 0) return 0;

        // Top
        count = ClipAgainstEdge(buffer2.Slice(0, count), buffer1, rect.Y + rect.Height, EdgeType.Top);
        if (count == 0) return 0;

        // Calculate Area of the final polygon in buffer1
        return CalculateArea(buffer1.Slice(0, count));
    }

    private enum EdgeType
    {
        Left,
        Right,
        Bottom,
        Top
    }

    private static int ClipAgainstEdge(ReadOnlySpan<Point> input, Span<Point> output, double edgePosition, EdgeType edgeType)
    {
        int outputCount = 0;
        int inputCount = input.Length;

        for (int i = 0; i < inputCount; i++)
        {
            var current = input[i];
            var next = input[(i + 1) % inputCount];

            bool currentInside = IsInside(current, edgePosition, edgeType);
            bool nextInside = IsInside(next, edgePosition, edgeType);

            if (currentInside && nextInside)
            {
                output[outputCount++] = next;
            }
            else if (currentInside && !nextInside)
            {
                output[outputCount++] = GetIntersection(current, next, edgePosition, edgeType);
            }
            else if (!currentInside && nextInside)
            {
                output[outputCount++] = GetIntersection(current, next, edgePosition, edgeType);
                output[outputCount++] = next;
            }
        }

        return outputCount;
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
                return new Point(edgePosition, p1.Y + (p2.Y - p1.Y) * (edgePosition - p1.X) / (p2.X - p1.X));
            case EdgeType.Bottom:
            case EdgeType.Top:
                return new Point(p1.X + (p2.X - p1.X) * (edgePosition - p1.Y) / (p2.Y - p1.Y), edgePosition);
            default:
                return p1;
        }
    }

    private static double CalculateArea(ReadOnlySpan<Point> vertices)
    {
        if (vertices.Length < 3)
            return 0;

        double sum = 0;

        for (int i = 0; i < vertices.Length; i++)
        {
            var current = vertices[i];
            var next = vertices[(i + 1) % vertices.Length];

            sum += current.X * next.Y - next.X * current.Y;
        }

        return Math.Abs(sum) / 2.0;
    }
    
    public static Polygon Intersect(Polygon polygon, Rect rect)
    {
        // Max vertices for a rect-rect intersection is usually small (max 8).
        // We use a buffer of 16 to be safe.
        Span<Point> buffer1 = stackalloc Point[16];
        Span<Point> buffer2 = stackalloc Point[16];

        // Copy initial vertices to buffer1
        int count = polygon.Vertices.Count;
        if (count > 16) throw new InvalidOperationException("Polygon has too many vertices for stack optimization");
        
        // Manual copy from List to Span
        for (int i = 0; i < count; i++)
        {
            buffer1[i] = polygon.Vertices[i];
        }

        // Clip against edges
        // We swap buffers: input -> output
        
        // Left
        count = ClipAgainstEdge(buffer1.Slice(0, count), buffer2, rect.X, EdgeType.Left);
        if (count == 0) return new Polygon(new List<Point>());

        // Right
        count = ClipAgainstEdge(buffer2.Slice(0, count), buffer1, rect.X + rect.Width, EdgeType.Right);
        if (count == 0) return new Polygon(new List<Point>());

        // Bottom
        count = ClipAgainstEdge(buffer1.Slice(0, count), buffer2, rect.Y, EdgeType.Bottom);
        if (count == 0) return new Polygon(new List<Point>());

        // Top
        count = ClipAgainstEdge(buffer2.Slice(0, count), buffer1, rect.Y + rect.Height, EdgeType.Top);
        if (count == 0) return new Polygon(new List<Point>());

        // Convert result Span back to List<Point> for Polygon constructor
        var resultList = new List<Point>(count);
        for (int i = 0; i < count; i++)
        {
            resultList.Add(buffer1[i]);
        }

        return new Polygon(resultList);
    }
}
