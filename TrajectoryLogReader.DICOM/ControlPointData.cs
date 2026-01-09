namespace TrajectoryLogReader.DICOM;

public class ControlPointData
{
    public int ControlPointIndex { get; set; }
    public float[,] MlcData { get; set; }
    public float CumulativeMetersetWeight { get; set; }
    public float X1 { get; set; }
    public float X2 { get; set; }
    public float Y1 { get; set; }
    public float Y2 { get; set; }
    public float GantryAngle { get; set; }
    public float CollimatorAngle { get; set; }
}