using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Complexity;

/// <summary>
/// Options for calculating the Average Leaf Pair Opening (ALPO).
/// </summary>
public class AverageLeafPairOpeningOptions
{
    /// <summary>
    /// When true, includes snapshots where the actual MU is less than or equal to zero.
    /// Default is false (excludes snapshots with no dose delivery).
    /// </summary>
    public bool IncludeZeroMu { get; set; } = false;

    /// <summary>
    /// When true, includes snapshots where the beam is on hold.
    /// Default is false (excludes beam hold snapshots).
    /// </summary>
    public bool IncludeBeamHold { get; set; } = false;

    /// <summary>
    /// The record type to use for calculations (Expected or Actual positions).
    /// Default is ActualPosition.
    /// </summary>
    public RecordType RecordType { get; set; } = RecordType.ActualPosition;
}
