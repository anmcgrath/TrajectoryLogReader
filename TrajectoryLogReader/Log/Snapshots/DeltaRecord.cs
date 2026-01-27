using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Snapshots;

internal class DeltaRecord : IScalarRecord
{
    private readonly int _measIndex;
    private readonly float _deltaConversion;
    private readonly TrajectoryLog _log;
    private readonly Axis _axis;
    private readonly AxisScale? _targetScale;

    /// <summary>
    /// Create a new delta record, that is the difference between this snapshot and the last, times by scale
    /// </summary>
    /// <param name="log"></param>
    /// <param name="axis"></param>
    /// <param name="measIndex"></param>
    /// <param name="deltaConversion">Converts the delta to a speed e.g gantry deg/s, conversion would be 1/0.02</param>
    /// <param name="targetScale"></param>
    internal DeltaRecord(TrajectoryLog log, Axis axis, int measIndex, float deltaConversion,
        AxisScale? targetScale = null)
    {
        _measIndex = measIndex;
        _deltaConversion = deltaConversion;
        _log = log;
        _axis = axis;
        _targetScale = targetScale;
    }

    public IScalarRecord WithScale(AxisScale scale)
    {
        // return new to be consistent but MU doesn't change with scale
        return new DeltaRecord(_log, _axis, _measIndex, _deltaConversion, scale);
    }

    // Raw values in native log scale (used internally)
    private float RawExpected
    {
        get
        {
            if (_measIndex == 0)
                return 0;

            var prev = _log.GetAxisData(_axis, _measIndex - 1, RecordType.ExpectedPosition);
            var curr = _log.GetAxisData(_axis, _measIndex, RecordType.ExpectedPosition);
            return Scale.Delta(SourceScale, prev, EffectiveScale, curr, _axis) * _deltaConversion;
        }
    }

    private float RawActual
    {
        get
        {
            if (_measIndex == 0)
                return 0;
            var prev = _log.GetAxisData(_axis, _measIndex - 1, RecordType.ActualPosition);
            var curr = _log.GetAxisData(_axis, _measIndex, RecordType.ActualPosition);
            return Scale.Delta(SourceScale, prev, EffectiveScale, curr, _axis) * _deltaConversion;
        }
    }

    private AxisScale SourceScale => _log.Header.AxisScale;
    private AxisScale EffectiveScale => _targetScale ?? SourceScale;

    public float Expected => _targetScale.HasValue
        ? Scale.Convert(SourceScale, _targetScale.Value, _axis, RawExpected)
        : RawExpected;

    public float Actual => _targetScale.HasValue
        ? Scale.Convert(SourceScale, _targetScale.Value, _axis, RawActual)
        : RawActual;

    public float GetRecord(RecordType type)
    {
        return type == RecordType.ExpectedPosition ? Expected : Actual;
    }

    public float Error => Scale.Delta(EffectiveScale, Expected, EffectiveScale, Actual, _axis);
}