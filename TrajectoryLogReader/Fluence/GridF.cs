using System.Numerics;
using System.Runtime.CompilerServices;

namespace TrajectoryLogReader.Fluence;

using System.Text;
using System.Threading.Tasks;

/// <summary>
/// A dense 2D grid of floating point values.
/// </summary>
public class GridF : IGrid<float>
{
    /// <summary>
    /// Total width of the grid in physical units (mm).
    /// </summary>
    public double Width => Bounds.Width;

    /// <summary>
    /// Total height of the grid in physical units (mm).
    /// </summary>
    public double Height => Bounds.Height;

    /// <summary>
    /// Resolution in X direction (mm / pixel).
    /// </summary>
    public double XRes { get; }

    /// <summary>
    /// Resolution in Y direction (mm / pixel).
    /// </summary>
    public double YRes { get; }

    /// <summary>
    /// Number of columns.
    /// </summary>
    public int Cols { get; }

    /// <summary>
    /// Number of rows.
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// The physical bounds of the grid.
    /// </summary>
    public Rect Bounds { get; }

    /// <summary>
    /// Access data with flat index [row * Cols + col].
    /// </summary>
    public float[] Data { get; }

    /// <summary>
    /// The maximum value in the grid
    /// </summary>
    /// <returns></returns>
    public float Max() => Data.Max();

    /// <summary>
    /// Gets or sets the data value at the specified row and column.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="col">The column index.</param>
    /// <returns>The data value.</returns>
    public float this[int row, int col]
    {
        get => Data[row * Cols + col];
        set => Data[row * Cols + col] = value;
    }

    public GridF(double width, double height, int cols, int rows) :
        this(new Rect() { X = -width / 2, Y = -height / 2, Width = width, Height = height }, cols, rows)
    {
        Cols = cols;
        Rows = rows;
        XRes = width / cols;
        YRes = height / rows;
        Data = new float[rows * cols];
        Bounds = new Rect() { X = -width / 2, Y = -height / 2, Width = width, Height = height };
    }

    public GridF(Rect bounds, int cols, int rows)
    {
        Cols = cols;
        Rows = rows;
        XRes = bounds.Width / cols;
        YRes = bounds.Height / rows;
        Data = new float[rows * cols];
        Bounds = bounds;
    }

    /// <summary>
    /// Returns the physical X coordinate for the given column index.
    /// </summary>
    public double GetX(int col)
    {
        return Bounds.X + col * XRes;
    }

    /// <summary>
    /// Returns the physical Y coordinate for the given row index.
    /// </summary>
    public double GetY(int row)
    {
        return Bounds.Y + row * YRes;
    }

    public float[] Flatten() => Data;

    /// <summary>
    /// Returns the data value at the specified column and row.
    /// </summary>
    public float GetData(int col, int row)
    {
        return this[row, col];
    }

    /// <summary>
    /// Sets the data value at the specified column and row.
    /// </summary>
    public void SetData(int col, int row, float value)
    {
        if (col < 0 || col >= Cols || row < 0 || row >= Rows)
            return;

        this[row, col] = value;
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

        int count = Data.Length;
        int vectorSize = Vector<float>.Count;
        int i = 0;

        // Process in SIMD chunks
        while (i <= count - vectorSize)
        {
            var v1 = new Vector<float>(Data, i);
            var v2 = new Vector<float>(other.Data, i);
            (v1 + v2).CopyTo(Data, i);
            i += vectorSize;
        }

        // Process remaining elements
        for (; i < count; i++)
        {
            Data[i] += other.Data[i];
        }
    }

    internal void DrawData(Span<Point> corners, AABB bounds, float value, bool useApproximate)
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

    private void DrawDataFastApproximate(Span<Point> corners, AABB bounds, float value)
    {
        // 1. Convert Corners to Grid Space (Pixels)
        Span<Point> gridCorners = stackalloc Point[4];
        for (int i = 0; i < 4; i++)
        {
            var gx = (corners[i].X - Bounds.X) / XRes;
            var gy = (corners[i].Y - Bounds.Y) / YRes;
            gridCorners[i] = new Point(gx, gy);
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

            int rowOffset = y * Cols;
            for (int x = c0; x <= c1; x++)
            {
                // Calculate coverage
                // Pixel x covers range [x, x+1]
                // Segment is [startX, endX]

                // Intersection of [x, x+1] and [startX, endX]
                var segMin = Math.Max(x, startX);
                var segMax = Math.Min(x + 1, endX);

                var coverage = segMax - segMin;

                if (coverage > 0)
                {
                    Data[rowOffset + x] += (float)(value * coverage);
                }
            }
        });
    }

    private void DrawDataFastExact(Span<Point> corners, AABB bounds, float value)
    {
        // 1. Convert Corners to Grid Space (Pixels)
        Span<Point> gridCorners = stackalloc Point[4];
        var invXRes = (1.0 / XRes);
        var invYRes = (1.0 / YRes);
        var offsetX = Bounds.X;
        var offsetY = Bounds.Y;

        for (int i = 0; i < 4; i++)
        {
            gridCorners[i] = new Point(
                (corners[i].X - offsetX) * invXRes,
                (corners[i].Y - offsetY) * invYRes);
        }

        // 2. Pre-compute edge equations for interior detection
        // For a convex quad, a point is inside if it's on the correct side of all 4 edges
        Span<double> edgeA = stackalloc double[4]; // edge normal X component
        Span<double> edgeB = stackalloc double[4]; // edge normal Y component
        Span<double> edgeC = stackalloc double[4]; // edge distance from origin

        for (int i = 0; i < 4; i++)
        {
            var p1 = gridCorners[i];
            var p2 = gridCorners[(i + 1) & 3]; // & 3 is faster than % 4

            // Edge direction
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;

            // Outward normal (perpendicular, pointing right of edge direction)
            // For CCW winding: normal = (-dy, dx)
            // For CW winding: normal = (dy, -dx)
            // We'll determine winding from the sign later
            edgeA[i] = -dy;
            edgeB[i] = dx;
            edgeC[i] = -(-dy * p1.X + dx * p1.Y);
        }

        // Determine winding by checking if center is "inside" with current normals
        var centerX = (gridCorners[0].X + gridCorners[2].X) * 0.5f;
        var centerY = (gridCorners[0].Y + gridCorners[2].Y) * 0.5f;
        var testSign = edgeA[0] * centerX + edgeB[0] * centerY + edgeC[0];
        var windingSign = testSign >= 0 ? 1d : -1d;

        // Flip normals if needed so "inside" is always positive
        if (windingSign < 0)
        {
            for (int i = 0; i < 4; i++)
            {
                edgeA[i] = -edgeA[i];
                edgeB[i] = -edgeB[i];
                edgeC[i] = -edgeC[i];
            }
        }

        // 3. Determine Row Range
        var boundsMinY = (bounds.MinY - offsetY) * invYRes;
        var boundsMaxY = (bounds.MaxY - offsetY) * invYRes;

        int y0 = Math.Max(0, (int)Math.Floor(boundsMinY));
        int y1 = Math.Min(Rows - 1, (int)Math.Ceiling(boundsMaxY));

        if (y0 > y1) return;

        // 4. Pre-compute row-edge intersections
        // For each edge, compute where it crosses each horizontal line y = row and y = row + 1
        int numRows = y1 - y0 + 1;
        Span<double> rowMinX = stackalloc double[numRows];
        Span<double> rowMaxX = stackalloc double[numRows];

        // Initialize with extreme values
        for (int i = 0; i < numRows; i++)
        {
            rowMinX[i] = double.MaxValue;
            rowMaxX[i] = double.MinValue;
        }

        // Process each edge
        for (int e = 0; e < 4; e++)
        {
            var p1 = gridCorners[e];
            var p2 = gridCorners[(e + 1) & 3];

            var minEdgeY = Math.Min(p1.Y, p2.Y);
            var maxEdgeY = Math.Max(p1.Y, p2.Y);

            // Skip horizontal edges (they don't contribute to X range)
            if (maxEdgeY - minEdgeY < 1e-6f) continue;

            var invDy = 1d / (p2.Y - p1.Y);

            // For each row this edge might affect
            int edgeRowStart = Math.Max(y0, (int)Math.Floor(minEdgeY));
            int edgeRowEnd = Math.Min(y1, (int)Math.Floor(maxEdgeY));

            for (int row = edgeRowStart; row <= edgeRowEnd; row++)
            {
                int ri = row - y0;

                // Check if edge crosses this row's vertical band [row, row+1]
                var rowTop = row + 1;
                var rowBot = row;

                // Include vertex contributions
                if (p1.Y >= rowBot && p1.Y <= rowTop)
                {
                    if (p1.X < rowMinX[ri]) rowMinX[ri] = p1.X;
                    if (p1.X > rowMaxX[ri]) rowMaxX[ri] = p1.X;
                }

                // Intersection with y = rowBot
                if ((p1.Y < rowBot && p2.Y > rowBot) || (p1.Y > rowBot && p2.Y < rowBot))
                {
                    var x = p1.X + (rowBot - p1.Y) * (p2.X - p1.X) * invDy;
                    if (x < rowMinX[ri]) rowMinX[ri] = x;
                    if (x > rowMaxX[ri]) rowMaxX[ri] = x;
                }

                // Intersection with y = rowTop
                if ((p1.Y < rowTop && p2.Y > rowTop) || (p1.Y > rowTop && p2.Y < rowTop))
                {
                    var x = p1.X + (rowTop - p1.Y) * (p2.X - p1.X) * invDy;
                    if (x < rowMinX[ri]) rowMinX[ri] = x;
                    if (x > rowMaxX[ri]) rowMaxX[ri] = x;
                }
            }
        }

        // Also check the last vertex
        for (int e = 0; e < 4; e++)
        {
            var p = gridCorners[e];
            int row = (int)Math.Floor(p.Y);
            if (row >= y0 && row <= y1)
            {
                int ri = row - y0;
                if (p.X < rowMinX[ri]) rowMinX[ri] = p.X;
                if (p.X > rowMaxX[ri]) rowMaxX[ri] = p.X;
            }
        }

        // 5. Process each row
        for (int row = y0; row <= y1; row++)
        {
            int ri = row - y0;

            // Skip rows with no intersection
            if (rowMinX[ri] > rowMaxX[ri]) continue;

            int startCol = Math.Max(0, (int)Math.Floor(rowMinX[ri]));
            int endCol = Math.Min(Cols - 1, (int)Math.Ceiling(rowMaxX[ri]));

            if (startCol > endCol) continue;

            int rowOffset = row * Cols;

            // 6. Find interior columns (pixels fully inside the polygon)
            // A pixel [col, col+1] x [row, row+1] is fully inside if all 4 corners are inside
            int interiorStart = -1;
            int interiorEnd = -1;

            // Check each column to find interior range
            for (int col = startCol; col <= endCol; col++)
            {
                if (IsPixelFullyInside(col, row, edgeA, edgeB, edgeC))
                {
                    if (interiorStart < 0) interiorStart = col;
                    interiorEnd = col;
                }
                else if (interiorStart >= 0)
                {
                    // We've exited the interior region
                    break;
                }
            }

            // 7. Process pixels
            if (interiorStart >= 0)
            {
                // Batch fill interior pixels
                for (int col = interiorStart; col <= interiorEnd; col++)
                {
                    Data[rowOffset + col] += value;
                }

                // Clip left edge pixels
                for (int col = startCol; col < interiorStart; col++)
                {
                    var area = Intersection.GetIntersectionAreaPixel(col, row, gridCorners);
                    if (area > 0)
                    {
                        Data[rowOffset + col] += (float)(value * area);
                    }
                }

                // Clip right edge pixels
                for (int col = interiorEnd + 1; col <= endCol; col++)
                {
                    var area = Intersection.GetIntersectionAreaPixel(col, row, gridCorners);
                    if (area > 0)
                    {
                        Data[rowOffset + col] += (float)(value * area);
                    }
                }
            }
            else
            {
                // No interior pixels - clip all
                for (int col = startCol; col <= endCol; col++)
                {
                    var area = Intersection.GetIntersectionAreaPixel(col, row, gridCorners);
                    if (area > 0)
                    {
                        Data[rowOffset + col] += (float)(value * area);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a pixel is fully inside the convex polygon defined by the edge equations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPixelFullyInside(int col, int row,
        ReadOnlySpan<double> edgeA, ReadOnlySpan<double> edgeB, ReadOnlySpan<double> edgeC)
    {
        // Check all 4 corners of the pixel
        double x0 = col, x1 = col + 1;
        double y0 = row, y1 = row + 1;

        // Small epsilon to handle floating-point precision issues at polygon boundaries
        const double epsilon = 1e-6d;

        // A point (x,y) is inside if edgeA[i]*x + edgeB[i]*y + edgeC[i] >= -epsilon for all edges
        for (int i = 0; i < 4; i++)
        {
            var a = edgeA[i];
            var b = edgeB[i];
            var c = edgeC[i];

            // Check all 4 corners - if any is outside this edge, pixel is not fully inside
            if (a * x0 + b * y0 + c < -epsilon) return false;
            if (a * x1 + b * y0 + c < -epsilon) return false;
            if (a * x0 + b * y1 + c < -epsilon) return false;
            if (a * x1 + b * y1 + c < -epsilon) return false;
        }

        return true;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int row = 0; row < Rows; row++)
        {
            var line = new StringBuilder();
            int rowOffset = row * Cols;
            for (int col = 0; col < Cols; col++)
            {
                line.Append(Data[rowOffset + col]);
                if (col != Cols - 1)
                    line.Append($"\t");
            }

            sb.AppendLine(line.ToString());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Bilinearly interpolates the grid value at the specified physical coordinates (x, y).
    /// </summary>
    /// <param name="x">X coordinate in mm.</param>
    /// <param name="y">Y coordinate in mm.</param>
    /// <param name="valIfNotFound">Value to return if coordinates are outside bounds.</param>
    /// <returns>Interpolated value.</returns>
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

        // Optimization: Pre-calculate row offsets
        int row1Offset = yi1 * Cols;
        int row2Offset = yi2 * Cols;

        var fX1Y1 = Data[row1Offset + xi1];
        var fX1Y2 = Data[row2Offset + xi1];
        var fX2Y1 = Data[row1Offset + xi2];
        var fX2Y2 = Data[row2Offset + xi2];

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