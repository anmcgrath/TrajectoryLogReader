namespace TrajectoryLogReader.DICOM.Plan;

/// <summary>
/// Represents a DICOM RT Plan.
/// </summary>
public class PlanModel
{
    public string PatientName { get; set; }
    public string PatientID { get; set; }
    public string PlanName { get; set; }
    public string SOPInstanceUID { get; set; }
    public string SeriesInstanceUID { get; set; }
    public string StudyInstanceUID { get; set; }
    public string PlanIntent { get; set; }
    public DateTime? PlanTimestamp { get; set; }
    public string PlanDescription { get; set; }
    public string TreatmentSite { get; set; }

    /// <summary>
    /// List of fraction groups.
    /// </summary>
    public List<FractionModel> Fractions { get; set; } = new();

    /// <summary>
    /// List of beams.
    /// </summary>
    public List<BeamModel> Beams { get; set; } = new();

    /// <summary>
    /// List of prescriptions.
    /// </summary>
    public List<PrescriptionModel> Prescriptions { get; set; } = new();
}