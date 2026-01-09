namespace TrajectoryLogReader.DICOM;

/// <summary>
/// The primary dosimeter unit.
/// </summary>
public enum PrimaryDosimeterUnit
{
    Unknown,
    /// <summary>
    /// Monitor Units.
    /// </summary>
    MU,
    /// <summary>
    /// Minutes (for Time based treatments).
    /// </summary>
    Minute
}
