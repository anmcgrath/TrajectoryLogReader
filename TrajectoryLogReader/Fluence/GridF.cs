namespace TrajectoryLogReader.Fluence;

using System.Text;

internal class GridF
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
    public void DrawData(RotatedRect rect, float value)
    {
        var col0 = GetCol(rect.Bounds.X);
        var row0 = GetRow(rect.Bounds.Y);
        var col1 = GetCol(rect.Bounds.X + rect.Bounds.Width);
        var row1 = GetRow(rect.Bounds.Y + rect.Bounds.Height);

        var areaPixel = XRes * YRes;

        for (int row = row0; row <= row1; row++)
        {
            for (int col = col0; col <= col1; col++)
            {
                var pixelRect = GetPixelBounds(col, row);
                var areaIntersection = Intersection.Intersect(rect.Polygon, pixelRect).Area();
                if (areaIntersection > 0)
                {
                    var currentData = GetData(col, row);

                    if (areaIntersection >= areaPixel)
                    {
                        currentData += value;
                    }
                    else if (areaIntersection > 0)
                    {
                        currentData += value * (float)(areaIntersection / areaPixel);
                    }

                    SetData(col, row, currentData);
                }
            }
        }
    }

    /// <summary>
    /// Draws the rect, rotated by <paramref name="angle"/>
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="angle"></param>
    /// <param name="value"></param>
    public void DrawData(Rect rect, double angle, float value)
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