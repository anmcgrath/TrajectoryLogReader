namespace TrajectoryLogReader.DICOM.Plan;

/// <summary>
/// Represents an applicator in a DICOM plan.
/// </summary>
public class ApplicatorModel
{
    /// <summary>
    /// The Applicator ID.
    /// </summary>
    public string ApplicatorID { get; set; }
    
    /// <summary>
    /// The Applicator Type.
    /// </summary>
    public string ApplicatorType { get; set; }
    
    /// <summary>
    /// The Applicator Description.
    /// </summary>
    public string ApplicatorDescription { get; set; }
}
