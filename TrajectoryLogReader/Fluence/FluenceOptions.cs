using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Fluence;

public class FluenceOptions
{
    /// <summary>
    /// The number of grid points in the x direction, defaults to 100
    /// </summary>
    public int GridCountX { get; set; } = 100;

    /// <summary>
    /// The number of grid points in the y direction, defaults to 100
    /// </summary>
    public int GridCountY { get; set; } = 100;

    /// <summary>
    /// The total grid size in cm in the x-direction.
    /// If left as the default, the size will be calculated from the max field size.
    /// </summary>
    public double GridSizeXInCm { get; set; } = -1;

    /// <summary>
    /// The total grid size in cm in the y-direction.
    /// If left as the default, the size will be calculated from the max field size.
    /// </summary>
    public double GridSizeYInCm { get; set; } = -1;

    /// <summary>
    /// The number of ms between each sample. Default is 20.
    /// Set to higher for less accurate but faster fluence generation.
    /// Must be a multiple of the log file sampling rate
    /// </summary>
    public int SampleRateInMs { get; set; } = 20;

    /// <summary>
    /// If true, uses a faster approximation (Scanline) algorithm for fluence calculation.
    /// If false (default), uses the exact intersection area calculation.
    /// </summary>
    public bool UseApproximateFluence { get; set; } = false;
}