using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Fluence;

public class FluenceOptions
{
    /// <summary>
    /// The number of grid points in the x direction, defaults to 100
    /// </summary>
    public int Cols { get; set; } = 100;

    /// <summary>
    /// The number of grid points in the y direction, defaults to 100
    /// </summary>
    public int Rows { get; set; } = 100;

    /// <summary>
    /// The total grid size (in mm) in the x-direction.
    /// If left as the default, the size will be calculated from the max field size.
    /// </summary>
    public double Width { get; set; } = -1;

    /// <summary>
    /// The total grid size (in mm) in the y-direction.
    /// If left as the default, the size will be calculated from the max field size.
    /// </summary>
    public double Height { get; set; } = -1;

    /// <summary>
    /// If true, uses a faster approximation (Scanline) algorithm for fluence calculation.
    /// If false (default), uses the exact intersection area calculation.
    /// </summary>
    public bool UseApproximateFluence { get; set; } = false;

    /// <summary>
    /// Minimum Delta MU required to include a snapshot in the fluence calculation.
    /// Useful for filtering out noise or transition artifacts. Default is 0.0001.
    /// </summary>
    public double MinDeltaMu { get; set; } = 0.0001;

    public FluenceOptions(int cols, int rows)
    {
        Cols = cols;
        Rows = rows;
    }

    public FluenceOptions()
    {
    }
}