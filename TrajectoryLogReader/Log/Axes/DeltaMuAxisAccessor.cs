using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Log.Axes;

internal class DeltaMuAxisAccessor : IAxisAccessor
{
    private readonly IAxisAccessor _muAccessor;
    public int TimeInMs => _muAccessor.TimeInMs;

    public DeltaMuAxisAccessor(IAxisAccessor muAccessor)
    {
        _muAccessor = muAccessor;
    }

    private IEnumerable<float> CalculateDeltaMu(IEnumerable<float> positions)
    {
        using var e = positions.GetEnumerator();
        if (!e.MoveNext()) yield break;

        float prev = e.Current;

        yield return 0f;

        while (e.MoveNext())
        {
            float current = e.Current;
            yield return current - prev;
            prev = current;
        }
    }

    public IEnumerable<float> ExpectedValues => CalculateDeltaMu(_muAccessor.ExpectedValues);

    public IEnumerable<float> ActualValues => CalculateDeltaMu(_muAccessor.ActualValues);

    public IEnumerable<float> ErrorValues
    {
        get
        {
            // Velocity Delta = ActualVelocity - ExpectedVelocity
            using var exp = ExpectedValues.GetEnumerator();
            using var act = ActualValues.GetEnumerator();

            while (exp.MoveNext() && act.MoveNext())
            {
                yield return act.Current - exp.Current;
            }
        }
    }

    public IAxisAccessor WithScale(AxisScale scale)
    {
        // mu is the same in all scales but return new to be consistent
        return new DeltaMuAxisAccessor(_muAccessor.WithScale(scale));
    }

    public float RootMeanSquareError()
    {
        return Statistics.CalculateRootMeanSquareError(ErrorValues);
    }

    public float MaxError()
    {
        return Statistics.CalculateMaxError(ErrorValues);
    }

    public Histogram ErrorHistogram(int nBins = 20)
    {
        return Histogram.FromData(ErrorValues.ToArray(), nBins);
    }
}