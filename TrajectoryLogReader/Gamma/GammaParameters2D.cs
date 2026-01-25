namespace TrajectoryLogReader.Gamma;

public class GammaParameters2D
{
    /// <summary>
    /// The distance to agreement tolerance (in mm)
    /// </summary>
    public double DtaTolMm { get; private set; }

    /// <summary>
    /// The dose difference tolerance (in %)
    /// </summary>
    public double DoseTolPercent { get; private set; }

    /// <summary>
    /// Whether doses are compared using the global dose to normalise in the gamma comparison.
    /// The global dose used for normalisation should be from the reference dose distribution.
    /// </summary>
    public bool Global { get; private set; }

    /// <summary>
    /// The threshold (in %) at which any dose below this will should not be counted in the gamma calculation.
    /// This is a % of the compared dose distribution.
    /// </summary>
    public double ThresholdPercent { get; private set; }

    /// <summary>
    /// If this is set, this limits the gamma search to the radius specified around each point.
    /// If not set, defaults as 2 * DTA tolerance
    /// </summary>
    public double? SearchRadius { get; set; }

    /// <summary>
    /// When the gamma search is performed, a supersampled grid is created. The resolution of this grid
    /// is given by DtaTol / SamplingRate. Default is 5, max is 10
    /// </summary>
    public int SamplingRate { get; set; } = 5;

    /// <summary>
    /// Create new parameters for a gamma comparison.
    /// </summary>
    /// <param name="dtaTolMm">The distance to agreement tolerance (in mm)</param>
    /// <param name="doseTolPercent">The dose difference tolerance (in %)</param>
    /// <param name="global">The threshold (in %) at which any dose below this will should not be counted in the gamma calculation.
    /// This is a % of the compared dose distribution.</param>
    /// <param name="thresholdPercent">The threshold (in %) at which any dose below this will should not be counted in the gamma calculation.
    /// This is a % of the compared dose distribution.</param>
    public GammaParameters2D(
        double dtaTolMm,
        double doseTolPercent,
        bool global = true,
        double thresholdPercent = 10)
    {
        if (dtaTolMm < 0.1)
            throw new Exception($"DTA tolerance must be greater than or equal to 0.1: {dtaTolMm}");
        
        DtaTolMm = dtaTolMm;
        DoseTolPercent = doseTolPercent;
        Global = global;
        ThresholdPercent = thresholdPercent;
    }

    /// <summary>
    /// Returns a string representation of the parameters.
    /// </summary>
    public string ToDetailsString()
    {
        var globalString = Global ? "Global" : "Local";
        return $"{DoseTolPercent}%, {DtaTolMm} mm, {ThresholdPercent}% Thr, {globalString}";
    }
}