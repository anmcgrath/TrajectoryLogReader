namespace TrajectoryLogReader.Gamma;

/// <summary>
/// Result of a 2D gamma analysis.
/// </summary>
public class GammaResult2D
{
    /// <summary>
    /// Parameters used for the analysis.
    /// </summary>
    public GammaParameters2D Parameters { get; }

    /// <summary>
    /// Fraction of points passing the gamma criteria (0.0 to 1.0).
    /// </summary>
    public double FracPass { get; }

    private readonly List<double> _x;
    private readonly List<double> _y;

    /// <summary>
    /// X coordinates of the gamma map.
    /// </summary>
    public IReadOnlyCollection<double> X => _x;

    /// <summary>
    /// Y coordinates of the gamma map.
    /// </summary>
    public IReadOnlyCollection<double> Y => _y;

    private float[,] GammaMap { get; }

    public GammaResult2D(GammaParameters2D parameters, double fracPass, List<double> x, List<double> y,
        float[,] gammaMap)
    {
        Parameters = parameters;
        FracPass = fracPass;
        _x = x;
        _y = y;
        GammaMap = gammaMap;
    }

    /// <summary>
    /// Calculates the median gamma value.
    /// </summary>
    public float Median() => GammaMap.Cast<float>().Where(x => x >= 0).Median();

    /// <summary>
    /// The 2D gamma map data.
    /// </summary>
    public float[,] Data => GammaMap;

    /// <summary>
    /// Gets the X coordinates.
    /// </summary>
    public IEnumerable<double> GetX() => X;

    /// <summary>
    /// Gets the Y coordinates.
    /// </summary>
    public IEnumerable<double> GetY() => Y;
}