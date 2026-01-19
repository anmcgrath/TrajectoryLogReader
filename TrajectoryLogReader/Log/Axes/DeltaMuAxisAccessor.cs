using System.Linq;

namespace TrajectoryLogReader.Log.Axes;

internal class DeltaMuAxisAccessor : AxisAccessorBase
{
    private readonly IAxisAccessor _muAccessor;
    public override int TimeInMs => _muAccessor.TimeInMs;

    private float[]? _expected;
    private float[]? _actual;
    private float[]? _errors;

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

    public override IEnumerable<float> ExpectedValues
    {
        get
        {
            if (_expected == null)
            {
                _expected = CalculateDeltaMu(_muAccessor.ExpectedValues).ToArray();
            }

            return _expected;
        }
    }

    public override IEnumerable<float> ActualValues
    {
        get
        {
            if (_actual == null)
            {
                _actual = CalculateDeltaMu(_muAccessor.ActualValues).ToArray();
            }

            return _actual;
        }
    }

    public override IEnumerable<float> ErrorValues
    {
        get
        {
            if (_errors == null)
            {
                _errors = GetErrors().ToArray();
            }

            return _errors;
        }
    }

    private IEnumerable<float> GetErrors()
    {
        // Velocity Delta = ActualVelocity - ExpectedVelocity
        using var exp = ExpectedValues.GetEnumerator();
        using var act = ActualValues.GetEnumerator();

        while (exp.MoveNext() && act.MoveNext())
        {
            yield return act.Current - exp.Current;
        }
    }

    public override IAxisAccessor WithScale(AxisScale scale)
    {
        // mu is the same in all scales but return new to be consistent
        return new DeltaMuAxisAccessor(_muAccessor.WithScale(scale));
    }
}