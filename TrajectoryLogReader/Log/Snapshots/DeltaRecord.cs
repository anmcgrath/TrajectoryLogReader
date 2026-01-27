using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Snapshots;

/// <summary>
/// Represents the delta (change) between two scalar records. Supports fluent chaining
/// for computing higher-order derivatives (e.g., velocity → acceleration).
/// </summary>
internal class DeltaRecord : IScalarRecord
{
    private readonly IScalarRecord? _previousRecord;
    private readonly IScalarRecord _currentRecord;
    private readonly float _timeConversion;
    private readonly Axis _axis;
    private readonly AxisScale? _targetScale;
    private readonly TrajectoryLog _log;

    /// <summary>
    /// Creates a delta record from two scalar records.
    /// </summary>
    /// <param name="previous">The previous record (null for first snapshot)</param>
    /// <param name="current">The current record</param>
    /// <param name="timeConversion">Multiplier to convert delta to rate (e.g., 1/0.02 for deg/s)</param>
    /// <param name="axis">The axis type (for rotational wrapping) and for scale conversion</param>
    /// <param name="targetScale">Optional scale override</param>
    /// <param name="log">The trajectory log reference</param>
    internal DeltaRecord(IScalarRecord? previous, IScalarRecord current,
        float timeConversion, Axis axis, AxisScale? targetScale, TrajectoryLog log)
    {
        _previousRecord = previous;
        _currentRecord = current;
        _timeConversion = timeConversion;
        _axis = axis;
        _targetScale = targetScale;
        _log = log;
    }

    public IScalarRecord WithScale(AxisScale scale)
    {
        // Recreate with new target scale - need to also apply scale to underlying records
        var prevScaled = _previousRecord?.WithScale(scale);
        var currScaled = _currentRecord.WithScale(scale);
        return new DeltaRecord(prevScaled, currScaled, _timeConversion, _axis, scale, _log);
    }

    private AxisScale SourceScale => _log.Header.AxisScale;
    private AxisScale EffectiveScale => _targetScale ?? SourceScale;

    public float Expected
    {
        get
        {
            if (_previousRecord == null)
                return 0;

            return Scale.Delta(EffectiveScale, _previousRecord.Expected,
                EffectiveScale, _currentRecord.Expected, _axis) * _timeConversion;
        }
    }

    public float Actual
    {
        get
        {
            if (_previousRecord == null)
                return 0;

            return Scale.Delta(EffectiveScale, _previousRecord.Actual,
                EffectiveScale, _currentRecord.Actual, _axis) * _timeConversion;
        }
    }

    public float GetRecord(RecordType type)
    {
        return type == RecordType.ExpectedPosition ? Expected : Actual;
    }

    public float Error => Scale.Delta(EffectiveScale, Expected, EffectiveScale, Actual, _axis);

    /// <summary>
    /// Gets the delta of this delta record (e.g., acceleration from velocity).
    /// </summary>
    public IScalarRecord GetDelta(TimeSpan? timeSpan = null)
    {
        // Recursively get the previous delta (velocity at t-1 for acceleration)
        var previousDelta = _previousRecord?.GetDelta(timeSpan);
        var msConverter = ComputeTimeConversion(timeSpan);
        return new DeltaRecord(previousDelta, this, msConverter, _axis, _targetScale, _log);
    }

    private float ComputeTimeConversion(TimeSpan? timeSpan)
    {
        return timeSpan.HasValue
            ? (float)(timeSpan.Value.TotalMilliseconds / _log.Header.SamplingIntervalInMS)
            : 1f;
    }
}