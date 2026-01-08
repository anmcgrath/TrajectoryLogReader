using System.Numerics;

namespace TrajectoryLogReader.Fluence;

using System.Text;
using System.Threading.Tasks;

public class GridF
{
    public double Width { get; }
    public double Height { get; }
    public double XRes { get; }
    public double YRes { get; }
    public int Cols { get; }
    public int Rows { get; }

    public Rect Bounds { get; }

    /// <summary>
    /// Access data with Data[yIndex, xIndex] where Data[0,0] is bottom LHS
    /// </summary>
    public float[,] Data { get; }

    public GridF(double width, double height, int cols, int rows)
    {
        Width = width;
        Height = height;
        Cols = cols;
        Rows = rows;
        XRes = width / cols;
        YRes = height / rows;
        Data = new float[rows, cols];
        Bounds = new Rect() { X = -width / 2, Y = -height / 2, Width = width, Height = height };
    }

    public double GetX(int col)
    {
        return Bounds.X + col * XRes;
    }

    public double GetY(int row)
    {
        return Bounds.Y + row * YRes;
    }

    public float GetData(int col, int row)
    {
        return Data[row, col];
    }

    public void SetData(int col, int row, float value)
    {
        if (col < 0 || col >= Cols || row < 0 || row >= Rows)
            return;

        Data[row, col] = value;
    }


    /// <summary>
    /// Returns the grid column that <paramref name="x"/> is inside. If outside, returns either 0 or <see cref="Cols"/> - 1 depending on which is closer
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public int GetCol(double x)
    {
        var col = (int)((x - Bounds.X) / XRes);
        if (col < 0)
            return 0;
        if (col >= Cols)
            return Cols - 1;

        return col;
    }

    /// <summary>
    /// Returns the grid row that <paramref name="y"/> is inside. If outside, returns either 0 or <see cref="Rows"/> - 1 depending on which is closer
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public int GetRow(double y)
    {
        var row = (int)((y - Bounds.Y) / YRes);
        if (row < 0)
            return 0;
        if (row >= Rows)
            return Rows - 1;

        return row;
    }

    /// <summary>
    /// Adds the values from another grid to this grid.
    /// Used for aggregating results from parallel execution.
    /// </summary>
    internal void Add(GridF other)
    {
        if (other.Cols != Cols || other.Rows != Rows)
            throw new ArgumentException("Grid dimensions must match");

        // Parallelize the merge
        Parallel.For(0, Rows, row =>
        {
            for (int col = 0; col < Cols; col++)
            {
                Data[row, col] += other.Data[row, col];
            }
        });
    }

    internal void DrawData(Span<Vector2> corners, AABB bounds, float value, bool useApproximate)
    {
        if (useApproximate)
        {
            DrawDataFastApproximate(corners, bounds, value);
        }
        else
        {
            DrawDataFastExact(corners, bounds, value);
        }
    }

    private void DrawDataFastApproximate(Span<Vector2> corners, AABB bounds, float value)
    {
        // 1. Convert Corners to Grid Space (Pixels)
        Span<Vector2> gridCorners = stackalloc Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            float gx = (float)((corners[i].X - Bounds.X) / XRes);
            float gy = (float)((corners[i].Y - Bounds.Y) / YRes);
            gridCorners[i] = new Vector2(gx, gy);
        }

        // 2. Calculate Clipping Bounds in Grid Space (Pixels)
        int clipMinY = 0;
        int clipMaxY = Rows - 1;

        // 3. Process Scanlines
        Scanline.ProcessScanlines(gridCorners, clipMinY, clipMaxY, (y, startX, endX) =>
        {
            // y is the row index.
            // startX and endX are column indices (float).

            // Validate row index just in case
            if (y < 0 || y >= Rows) return;

            // Determine integer column range
            int colStart = (int)Math.Floor(startX);
            int colEnd = (int)Math.Floor(endX);

            // Clamp columns to grid
            if (colEnd < 0) return;
            if (colStart >= Cols) return;

            int c0 = Math.Max(0, colStart);
            int c1 = Math.Min(Cols - 1, colEnd);

            for (int x = c0; x <= c1; x++)
            {
                // Calculate coverage
                // Pixel x covers range [x, x+1]
                // Segment is [startX, endX]

                // Intersection of [x, x+1] and [startX, endX]
                float segMin = Math.Max(x, startX);
                float segMax = Math.Min(x + 1, endX);

                float coverage = segMax - segMin;

                if (coverage > 0)
                {
                    Data[y, x] += value * coverage;
                }
            }
        });
    }

    private void DrawDataFastExact(Span<Vector2> corners, AABB bounds, float value)
    {
        // 1. Convert Corners to Grid Space (Pixels)
        Span<Vector2> gridCorners = stackalloc Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            float gx = (float)((corners[i].X - Bounds.X) / XRes);
            float gy = (float)((corners[i].Y - Bounds.Y) / YRes);
            gridCorners[i] = new Vector2(gx, gy);
        }

        // 2. Determine Row Range
        // We need to cover the full vertical extent of the polygon in grid space
        // Bounds are also in World Space, convert them
        float boundsMinY = (float)((bounds.MinY - Bounds.Y) / YRes);
        float boundsMaxY = (float)((bounds.MaxY - Bounds.Y) / YRes);

        int y0 = Math.Max(0, (int)Math.Floor(boundsMinY));
        int y1 = Math.Min(Rows - 1, (int)Math.Ceiling(boundsMaxY));

        var areaPixel = 1.0f; // In grid space, pixel area is 1x1 = 1.

        for (int row = y0; row <= y1; row++)
        {
            // Calculate X Range for this row (y to y+1)
            GetXRangeFast(gridCorners, row, row + 1, out float minX, out float maxX);

            int startCol = Math.Max(0, (int)Math.Floor(minX));
            int endCol = Math.Min(Cols - 1, (int)Math.Ceiling(maxX));

            for (int col = startCol; col <= endCol; col++)
            {
                // Pixel bounds in Grid Space are simply (col, row, col+1, row+1)
                var pixelRect = new AABB(col, row, col + 1, row + 1);

                var areaIntersection = Intersection.GetIntersectionArea(pixelRect, gridCorners);

                if (areaIntersection > 0)
                {
                    if (areaIntersection >= areaPixel - 1e-9)
                    {
                        Data[row, col] += value;
                    }
                    else
                    {
                        Data[row, col] +=
                            value * areaIntersection; // areaIntersection is fraction since pixel area is 1
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calculates the min and max X values of the polygon within the given Y range (Optimized for Span).
    /// </summary>
    private void GetXRangeFast(ReadOnlySpan<Vector2> vertices, float yMin, float yMax, out float minX, out float maxX)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;

        int count = vertices.Length;
        for (int i = 0; i < count; i++)
        {
            var p1 = vertices[i];
            var p2 = vertices[(i + 1) % count];

            // Check if edge intersects the Y band
            // Case 1: Both points above or below - skip (unless one is exactly on boundary)
            if (Math.Max(p1.Y, p2.Y) < yMin || Math.Min(p1.Y, p2.Y) > yMax)
                continue;

            // Include vertices inside the band
            if (p1.Y >= yMin && p1.Y <= yMax)
            {
                if (p1.X < minX) minX = p1.X;
                if (p1.X > maxX) maxX = p1.X;
            }

            if (p2.Y >= yMin && p2.Y <= yMax)
            {
                if (p2.X < minX) minX = p2.X;
                if (p2.X > maxX) maxX = p2.X;
            }

            // Intersect with yMin
            if ((p1.Y < yMin && p2.Y > yMin) || (p1.Y > yMin && p2.Y < yMin))
            {
                float x = p1.X + (yMin - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y);
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }

            // Intersect with yMax
            if ((p1.Y < yMax && p2.Y > yMax) || (p1.Y > yMax && p2.Y < yMax))
            {
                float x = p1.X + (yMax - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y);
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }

        // Fallback if no intersection found
        if (minX > maxX)
        {
            minX = 0;
            maxX = 0;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int row = 0; row < Rows; row++)
        {
            var line = new StringBuilder();
            for (int col = 0; col < Cols; col++)
            {
                line.Append(GetData(col, row));
                if (col != Cols - 1)
                    line.Append($"\t");
            }

            sb.AppendLine(line.ToString());
        }

        return sb.ToString();
    }

    public float Interpolate(double x, double y, float valIfNotFound = 0)
    {
        if (!Bounds.Contains(x, y))
            return valIfNotFound;

        if (Cols <= 1 || Rows <= 1)
            return valIfNotFound;

        var xi1 = GetCol(x);
        var yi1 = GetRow(y);

        // Ensure we don't go out of bounds for the second point
        if (xi1 >= Cols - 1) xi1 = Cols - 2;
        if (yi1 >= Rows - 1) yi1 = Rows - 2;

        var xi2 = xi1 + 1;
        var yi2 = yi1 + 1;

        var x1 = GetX(xi1);
        var x2 = GetX(xi2);

        var y1 = GetY(yi1);
        var y2 = GetY(yi2);

        var fX1Y1 = Data[yi1, xi1];
        var fX1Y2 = Data[yi2, xi1];
        var fX2Y1 = Data[yi1, xi2];
        var fX2Y2 = Data[yi2, xi2];

        return InterpXy(x, y, x1, x2, fX1Y1, fX2Y1, fX1Y2, fX2Y2, y1, y2);
    }

    private static float InterpXy(double x, double y, double x1, double x2, double fX1Y1, double fX2Y1,
        float fX1Y2,
        float fX2Y2, double y1, double y2)
    {
        var fxY1 = (x2 - x) / (x2 - x1) * fX1Y1 + (x - x1) / (x2 - x1) * fX2Y1;
        var fxY2 = (x2 - x) / (x2 - x1) * fX1Y2 + (x - x1) / (x2 - x1) * fX2Y2;
        var fxy = (y2 - y) / (y2 - y1) * fxY1 + (y - y1) / (y2 - y1) * fxY2;

        return (float)fxy;
    }
}