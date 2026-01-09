using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.DICOM;

public class BeamModel
{
    public string BeamName { get; set; }
    public int BeamNumber { get; set; }
    public PrimaryDosimeterUnit PrimaryDosimeterUnit { get; set; }
    public int NumberOfControlPoints { get; set; }
    public float Energy { get; set; }
    public RadiationType RadiationType { get; set; }
    public BeamType BeamType { get; set; }
    public string BeamDescription { get; set; }
    public string TreatmentMachineName { get; set; }
    public FluenceMode PrimaryFluenceMode { get; set; }
    public List<ApplicatorModel> Applicators { get; set; } = new();
    public List<BlockModel> Blocks { get; set; } = new();
    public List<WedgeModel> Wedges { get; set; } = new();
    public List<ControlPointData> ControlPoints { get; set; } = new();
    public float MU { get; set; }
    public IMLCModel Mlc { get; set; }
}