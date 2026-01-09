using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.DICOM;

public class BeamModel
{
    public string BeamName { get; set; }
    public int BeamNumber { get; set; }
    public string PrimaryDosimeterUnit { get; set; }
    public int NumberOfControlPoints { get; set; }
    public float Energy { get; set; }
    public List<ControlPointData> ControlPoints { get; set; } = new();
    public float MU { get; set; }
    public IMLCModel Mlc { get; set; }
}