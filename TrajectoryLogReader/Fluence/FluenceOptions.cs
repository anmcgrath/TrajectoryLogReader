using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Fluence;

/// <summary>
/// Configures how fluence is reconstructed from delivery snapshots. These settings control
/// grid resolution, spatial extent, and the trade-off between geometric accuracy and speed.
/// Distances are expressed in millimeters.
/// </summary>
public class FluenceOptions
{
    /// <summary>
    /// The number of grid columns (X direction). Higher values improve spatial resolution
    /// at the cost of CPU time and memory. Default is 100.
    /// </summary>
    public int Cols { get; set; } = 100;

    /// <summary>
    /// The number of grid rows (Y direction). Higher values improve spatial resolution
    /// at the cost of CPU time and memory. Default is 100.
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

    /// <summary>
    /// Margin (in mm) to add to each side of the automatically calculated grid bounds.
    /// Only applies when Width/Height are not explicitly specified. Default is 10 mm.
    /// </summary>
    public double Margin { get; set; } = 10;

    /// <summary>
    /// Set this to override the collimator angle to a fixed value
    /// </summary>
    public float? FixedCollimatorAngle { get; set; } = null;

    /// <summary>
    /// Sets how many processors to use for fluence creation
    /// </summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Creates options with an explicit grid resolution.
    /// </summary>
    /// <param name="cols">The number of grid columns (X direction).</param>
    /// <param name="rows">The number of grid rows (Y direction).</param>
    public FluenceOptions(int cols, int rows)
    {
        Cols = cols;
        Rows = rows;
    }

    /// <summary>
    /// Creates options using the default grid resolution and bounds behavior.
    /// </summary>
    public FluenceOptions()
    {
    }
}
