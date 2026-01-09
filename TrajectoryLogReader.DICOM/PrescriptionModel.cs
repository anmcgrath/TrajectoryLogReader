namespace TrajectoryLogReader.DICOM;

public class PrescriptionModel
{
    public int DoseReferenceNumber { get; set; }
    public string DoseReferenceStructureType { get; set; }
    public string DoseReferenceDescription { get; set; }
    public string DoseReferenceType { get; set; }
    public float TargetPrescriptionDose { get; set; }
}
