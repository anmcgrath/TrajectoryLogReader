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
        DtaTolMm = dtaTolMm;
        DoseTolPercent = doseTolPercent;
        Global = global;
        ThresholdPercent = thresholdPercent;
    }

    public string ToDetailsString()
    {
        var globalString = Global ? "Global" : "Local";
        return $"{DoseTolPercent}%, {DtaTolMm} mm, {ThresholdPercent}% Thr, {globalString}";
    }
}