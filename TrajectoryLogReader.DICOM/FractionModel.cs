namespace TrajectoryLogReader.DICOM;

/// <summary>
/// Represents a fraction group in a DICOM plan.
/// </summary>
public class FractionModel
{
    /// <summary>
    /// The fraction group number.
    /// </summary>
    public int FractionGroupNumber { get; set; }
    
    /// <summary>
    /// Number of fractions planned.
    /// </summary>
    public int NumberOfFractionsPlanned { get; set; }
    
    /// <summary>
    /// Number of beams in the fraction group.
    /// </summary>
    public int NumberOfBeams { get; set; }
}
