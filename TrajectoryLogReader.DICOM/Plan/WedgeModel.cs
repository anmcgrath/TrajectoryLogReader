namespace TrajectoryLogReader.DICOM;

/// <summary>
/// Represents a wedge in a DICOM plan.
/// </summary>
public class WedgeModel
{
    /// <summary>
    /// The Wedge ID.
    /// </summary>
    public string WedgeID { get; set; }
    
    /// <summary>
    /// The Wedge Type.
    /// </summary>
    public string WedgeType { get; set; }
    
    /// <summary>
    /// The Wedge Number.
    /// </summary>
    public int WedgeNumber { get; set; }
    
    /// <summary>
    /// The Wedge Angle.
    /// </summary>
    public float WedgeAngle { get; set; }
}
