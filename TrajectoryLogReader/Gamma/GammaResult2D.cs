namespace TrajectoryLogReader.Gamma;

public class GammaResult2D
{
    public GammaParameters2D Parameters { get; }
    public double FracPass { get; }

    private readonly List<double> _x;
    private readonly List<double> _y;

    public IReadOnlyCollection<double> X => _x;
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

    public float Median() => GammaMap.Cast<float>().Where(x => x >= 0).Median();

    public float[,] Data => GammaMap;

    public IEnumerable<double> GetX() => X;

    public IEnumerable<double> GetY() => Y;
}