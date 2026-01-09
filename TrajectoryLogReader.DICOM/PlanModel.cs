namespace TrajectoryLogReader.DICOM;

public class PlanModel
{
    public string PatientName { get; set; }
    public string PatientID { get; set; }
    public string PlanName { get; set; }
    public List<FractionModel> Fractions { get; set; } = new();
    public List<BeamModel> Beams { get; set; } = new();
    public List<PrescriptionModel> Prescriptions { get; set; } = new();
}