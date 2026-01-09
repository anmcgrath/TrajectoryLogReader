namespace TrajectoryLogReader.DICOM;

/// <summary>
/// Represents a prescription in a DICOM plan.
/// </summary>
public class PrescriptionModel
{
    public int DoseReferenceNumber { get; set; }
    public string DoseReferenceStructureType { get; set; }
    public string DoseReferenceDescription { get; set; }
    public string DoseReferenceType { get; set; }
    public float TargetPrescriptionDose { get; set; }
}
