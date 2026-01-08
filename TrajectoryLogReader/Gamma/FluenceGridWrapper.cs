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
    public double XRes => _grid.XRes * 10;
    public double YRes => _grid.YRes * 10;
    public double XMin => _grid.Bounds.X * 10;
    public double XMax => _grid.Bounds.X * 10 + _grid.Bounds.Width * 10;
    public double YMin => _grid.Bounds.Y * 10;
    public double YMax => _grid.Bounds.Y * 10 + _grid.Bounds.Height * 10;

    public float Max() => _grid.Data.Cast<float>().Max();

    public float Interpolate(double x, double y, float valIfNotFound) =>
        _grid.Interpolate(x / 10, y / 10, valIfNotFound);

    public float GetValue(int row, int col) => _grid.GetData(row, col);
    public double GetX(int col) => _grid.GetX(col) * 10;

    public double GetY(int row) => _grid.GetY(row) * 10;

    public bool Contains(double x, double y) => _grid.Bounds.Contains(x / 10, y / 10);
    public float[] Flatten() => _grid.Data.Cast<float>().ToArray();

    public float[,] Data => _grid.Data;
    public IEnumerable<double> GetX() => Enumerable.Range(0, _grid.Cols).Select(GetX);
    public IEnumerable<double> GetY() => Enumerable.Range(0, _grid.Rows).Select(GetY);
}