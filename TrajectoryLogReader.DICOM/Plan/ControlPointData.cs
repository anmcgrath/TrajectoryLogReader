namespace TrajectoryLogReader.DICOM.Plan;

/// <summary>
/// Data for a single control point in a DICOM plan.
/// </summary>
public class ControlPointData
{
    public int ControlPointIndex { get; set; }

    /// <summary>
    /// MLC positions.
    /// </summary>
    public float[,]? MlcData { get; set; }

    /// <summary>
    /// Cumulative meterset weight.
    /// </summary>
    public float CumulativeMetersetWeight { get; set; }

    public float? X1 { get; set; }
    public float? X2 { get; set; }
    public float? Y1 { get; set; }
    public float? Y2 { get; set; }

    public bool AreJawsInvalid() => X1 == null || X2 == null || Y1 == null || Y2 == null;

    /// <summary>
    /// Gantry angle in degrees.
    /// </summary>
    public float? GantryAngle { get; set; }

    /// <summary>
    /// Collimator angle in degrees.
    /// </summary>
    public float? CollimatorAngle { get; set; }
}