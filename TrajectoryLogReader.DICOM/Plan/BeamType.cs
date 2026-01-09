namespace TrajectoryLogReader.DICOM.Plan;

/// <summary>
/// The type of beam delivery.
/// </summary>
public enum BeamType
{
    Unknown,
    /// <summary>
    /// Static beam.
    /// </summary>
    Static,
    /// <summary>
    /// Dynamic beam (e.g. VMAT, sliding window).
    /// </summary>
    Dynamic
}
