using System.Linq;

namespace TrajectoryLogReader.Log.Axes;

internal class DeltaAxisAccessor : AxisAccessorBase
{
    private readonly IAxisAccessor _innerAccessor;
    private readonly double _sampleRateInMs;
    private readonly TimeSpan? _timeSpan;
    public override int TimeInMs => _innerAccessor.TimeInMs;
    public override int SampleRateInMs => _innerAccessor.SampleRateInMs;

    /// <summary>
    /// The inner axis, if it has one
    /// </summary>
    private Axis? _innerAxis;


    private float[]? _expected;
    private float[]? _actual;
    private float[]? _errors;
    private readonly float _deltaMultiplier;

    public DeltaAxisAccessor(IAxisAccessor innerAccessor, double sampleRateInMs, TimeSpan? timeSpan = null)
    {
        _innerAccessor = innerAccessor;
        _sampleRateInMs = sampleRateInMs;
        _timeSpan = timeSpan;
        _deltaMultiplier = timeSpan == null ? 1 : (float)(timeSpan.Value.TotalMilliseconds / sampleRateInMs);
        if (innerAccessor is IOriginalAxisAccessor o)
            _innerAxis = o.Axis;
    }

    private IEnumerable<float> CalculateDelta(IEnumerable<float> positions)
    {
        using var e = positions.GetEnumerator();
        if (!e.MoveNext()) yield break;

        float prev = e.Current;

        yield return 0f;

        while (e.MoveNext())
        {
            float current = e.Current;

            var difference = current - prev;
            if (_innerAxis.HasValue && _innerAxis.Value.IsRotational())
                difference = Normalize(difference, 360f);

            yield return difference * _deltaMultiplier;
            prev = current;
        }
    }

    public override IEnumerable<float> ExpectedValues
    {
        get
        {
            if (_expected == null)
            {
                _expected = CalculateDelta(_innerAccessor.ExpectedValues).ToArray();
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
                _actual = CalculateDelta(_innerAccessor.ActualValues).ToArray();
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
        return new DeltaAxisAccessor(_innerAccessor.WithScale(scale), _sampleRateInMs, _timeSpan);
    }

    public override AxisScale GetSourceScale() => _innerAccessor.GetSourceScale();

    public override AxisScale GetEffectiveScale() => _innerAccessor.GetEffectiveScale();

    private static float Normalize(float value, float period)
    {
        if (value > period / 2) return value - period;
        if (value <= -period / 2) return value + period;
        return value;
    }

}
