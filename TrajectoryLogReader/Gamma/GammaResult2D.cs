using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Gamma;

/// <summary>
/// Result of a 2D gamma analysis.
/// </summary>
public class GammaResult2D
{
    /// <summary>
    /// Parameters used for the analysis.
    /// </summary>
    public GammaParameters2D Parameters { get; }

    /// <summary>
    /// Fraction of points passing the gamma criteria (0.0 to 1.0).
    /// </summary>
    public double FracPass { get; }

    /// <summary>
    /// The 2D gamma map data grid.
    /// </summary>
    public GridF Grid { get; }

    public GammaResult2D(GammaParameters2D parameters, double fracPass, GridF grid)
    {
        Parameters = parameters;
        FracPass = fracPass;
        Grid = grid;
    }

    /// <summary>
    /// Calculates the median gamma value.
    /// </summary>
    public float Median() => Grid.Data.Cast<float>().Where(x => x >= 0).Median();

    /// <summary>
    /// The 2D gamma map data (flattened).
    /// </summary>
    public float[] Data => Grid.Data;

    /// <summary>
    /// Gets the X coordinates.
    /// </summary>
    public IEnumerable<double> GetX() => Enumerable.Range(0, Grid.Cols).Select(Grid.GetX);

    /// <summary>
    /// Gets the Y coordinates.
    /// </summary>
    public IEnumerable<double> GetY() => Enumerable.Range(0, Grid.Rows).Select(Grid.GetY);
}