namespace TrajectoryLogReader.DICOM;

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
    public List<FractionModel> Fractions { get; set; } = new();
    public List<BeamModel> Beams { get; set; } = new();
    public List<PrescriptionModel> Prescriptions { get; set; } = new();
}