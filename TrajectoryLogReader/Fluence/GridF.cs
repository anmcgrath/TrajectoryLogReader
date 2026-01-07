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
    public int SizeX { get; }
    public int SizeY { get; }

    public Rect Bounds { get; }

    /// <summary>
    /// Access data with Data[yIndex, xIndex] where Data[0,0] is bottom LHS
    /// </summary>
    public float[,] Data { get; }

    public GridF(double width, double height, int sizeX, int sizeY)
    {
        Width = width;
        Height = height;
        SizeX = sizeX;
        SizeY = sizeY;
        XRes = width / sizeX;
        YRes = height / sizeY;
        Data = new float[sizeY, sizeX];
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
        if (col < 0 || col >= SizeX || row < 0 || row >= SizeY)
            return;

        Data[row, col] = value;
    }

    /// <summary>
    /// Gets the pixel bounds at <paramref name="col"/>, <paramref name="row"/>
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    private Rect GetPixelBounds(int col, int row)
    {
        return new Rect()
        {
            X = GetX(col),
            Y = GetY(row),
            Width = XRes,
            Height = YRes
        };
    }

    /// <summary>
    /// Gets the pixel bounds at <paramref name="col"/>, <paramref name="row"/>
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    private AABB GetPixelBoundsAABB(int col, int row)
    {
        var minX = (float)GetX(col);
        var minY = (float)GetY(row);
        var maxX = minX + (float)XRes;
        var maxY = minY + (float)YRes;
        return new AABB(minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Returns the grid column that <paramref name="x"/> is inside. If outside, returns either 0 or <see cref="SizeX"/> - 1 depending on which is closer
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public int GetCol(double x)
    {
        var col = (int)((x - Bounds.X) / XRes);
        if (col < 0)
            return 0;
        if (col >= SizeX)
            return SizeX - 1;

        return col;
    }

    /// <summary>
    /// Returns the grid row that <paramref name="y"/> is inside. If outside, returns either 0 or <see cref="SizeY"/> - 1 depending on which is closer
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public int GetRow(double y)
    {
        var row = (int)((y - Bounds.Y) / YRes);
        if (row < 0)
            return 0;
        if (row >= SizeY)
            return SizeY - 1;

        return row;
    }

    /// <summary>
    /// Adds <paramref name="value"/> to the grid cells covered by <paramref name="rect"/>. Partially covered grid cells
    /// will add the fractional area where the rect covers the pixel.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="value"></param>
    private void DrawData(RotatedRect rect, float value)
    {
        var row0 = GetRow(rect.Bounds.Y);
        var row1 = GetRow(rect.Bounds.Y + rect.Bounds.Height);

        var areaPixel = XRes * YRes;
        var polygonVertices = rect.Polygon.Vertices;

        // Use Parallel.For to speed up the processing of rows
        Parallel.For(row0, row1 + 1, row =>
        {
            // Scanline optimization:
            // Calculate the X range of the polygon for this row's Y band.
            // This avoids iterating over empty pixels in the bounding box.

            double yBottom = GetY(row);
            double yTop = yBottom + YRes;

            GetXRange(polygonVertices, yBottom, yTop, out double minX, out double maxX);

            // Convert X range to columns
            int startCol = GetCol(minX);
            int endCol = GetCol(maxX);

            // Ensure we don't go out of bounds of the grid or the rect's bounding box
            // (GetCol already clamps to 0..SizeX-1)

            for (int col = startCol; col <= endCol; col++)
            {
                var pixelRect = GetPixelBounds(col, row);

                // Optimization: Check if pixel is fully inside the X range (conservative)
                // If the pixel is strictly between minX and maxX (with some margin), it might be fully inside.
                // However, the edges are slanted, so minX/maxX are the extremes for the whole row.
                // A pixel at the edge might still be partially covered.

                // Use the allocation-free intersection area calculation
                var areaIntersection = Intersection.GetIntersectionArea(polygonVertices, pixelRect);

                if (areaIntersection > 0)
                {
                    if (areaIntersection >= areaPixel - 1e-9) // Tolerance for float precision
                    {
                        Data[row, col] += value;
                    }
                    else
                    {
                        Data[row, col] += value * (float)(areaIntersection / areaPixel);
                    }
                }
            }
        });
    }

    public void DrawDataFast(Span<Vector2> corners, AABB bounds, float value)
    {
        // 1. Convert Corners to Grid Space (Pixels)
        // We use stackalloc for the grid-space corners to avoid GC
        Span<Vector2> gridCorners = stackalloc Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            float gx = (float)((corners[i].X - Bounds.X) / XRes);
            float gy = (float)((corners[i].Y - Bounds.Y) / YRes);
            gridCorners[i] = new Vector2(gx, gy);
        }

        // 2. Calculate Clipping Bounds in Grid Space (Pixels)
        // We clamp to the grid dimensions (0 to SizeY)
        // Scanline expects integer Y range.
        int clipMinY = 0;
        int clipMaxY = SizeY - 1;

        // 3. Process Scanlines
        Scanline.ProcessScanlines(gridCorners, clipMinY, clipMaxY, (y, startX, endX) =>
        {
            // y is the row index.
            // startX and endX are column indices (float).
            
            // Validate row index just in case
            if (y < 0 || y >= SizeY) return;

            // Determine integer column range
            int colStart = (int)Math.Floor(startX);
            int colEnd = (int)Math.Floor(endX);

            // Clamp columns to grid
            if (colEnd < 0) return;
            if (colStart >= SizeX) return;

            int c0 = Math.Max(0, colStart);
            int c1 = Math.Min(SizeX - 1, colEnd);

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

    /// <summary>
    /// Calculates the min and max X values of the polygon within the given Y range.
    /// </summary>
    private void GetXRange(List<Point> vertices, double yMin, double yMax, out double minX, out double maxX)
    {
        minX = double.MaxValue;
        maxX = double.MinValue;

        int count = vertices.Count;
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
                double x = p1.X + (yMin - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y);
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }

            // Intersect with yMax
            if ((p1.Y < yMax && p2.Y > yMax) || (p1.Y > yMax && p2.Y < yMax))
            {
                double x = p1.X + (yMax - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y);
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }

        // Fallback if no intersection found (should not happen if row is within bounds)
        if (minX > maxX)
        {
            minX = 0;
            maxX = 0;
        }
    }

    /// <summary>
    /// Draws the rect, rotated by <paramref name="angle"/>
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="angle"></param>
    /// <param name="value"></param>
    internal void DrawData(Rect rect, double angle, float value)
    {
        DrawData(RotatedRect.Create(rect, angle), value);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int row = 0; row < SizeY; row++)
        {
            var line = new StringBuilder();
            for (int col = 0; col < SizeX; col++)
            {
                line.Append(GetData(col, row));
                if (col != SizeX - 1)
                    line.Append($"\t");
            }

            sb.AppendLine(line.ToString());
        }

        return sb.ToString();
    }
}