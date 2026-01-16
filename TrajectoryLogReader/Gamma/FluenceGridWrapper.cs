using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Gamma;

internal class FluenceGridWrapper : IGrid<float>
{
    private readonly GridF _grid;

    public FluenceGridWrapper(GridF grid)
    {
        _grid = grid;
    }

    public int Cols => _grid.Cols;
    public int Rows => _grid.Rows;
    public double XRes => _grid.XRes;
    public double YRes => _grid.YRes;
    public double XMin => _grid.Bounds.X;
    public double XMax => _grid.Bounds.X + _grid.Bounds.Width;
    public double YMin => _grid.Bounds.Y;
    public double YMax => _grid.Bounds.Y + _grid.Bounds.Height;

    public float Max() => _grid.Data.Max();

    public float Interpolate(double x, double y, float valIfNotFound) =>
        _grid.Interpolate(x, y, valIfNotFound);

    public float GetValue(int row, int col) => _grid.GetData(row, col);
    public double GetX(int col) => _grid.GetX(col);

    public double GetY(int row) => _grid.GetY(row);

    public bool Contains(double x, double y) => _grid.Bounds.Contains(x, y);
    public float[] Flatten() => _grid.Data.ToArray();

    public float[] Data => _grid.Data;
    public IEnumerable<double> GetX() => Enumerable.Range(0, _grid.Cols).Select(GetX);
    public IEnumerable<double> GetY() => Enumerable.Range(0, _grid.Rows).Select(GetY);
}