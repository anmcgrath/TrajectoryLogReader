using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Snapshots;

public class DeltaMuRecord : IScalarRecord
{
    private readonly int _measIndex;
    private readonly TrajectoryLog _log;
    private readonly Axis _axis;
    private readonly AxisScale? _targetScale;

    internal DeltaMuRecord(TrajectoryLog log, Axis axis, int measIndex, AxisScale? targetScale = null)
    {
        _measIndex = measIndex;
        _log = log;
        _axis = axis;
        _targetScale = targetScale;
    }

    public IScalarRecord WithScale(AxisScale scale)
    {
        // return new to be consistent but MU doesn't change with scale
        return new DeltaMuRecord(_log, _axis, _measIndex, scale);
    }

    // Raw values in native log scale (used internally)
    private float RawExpected
    {
        get
        {
            if (_measIndex == 0)
                return 0;
            return _log.GetAxisData(_axis, _measIndex, RecordType.ExpectedPosition) -
                   _log.GetAxisData(_axis, _measIndex - 1, RecordType.ExpectedPosition);
        }
    }

    private float RawActual
    {
        get
        {
            if (_measIndex == 0)
                return 0;
            return _log.GetAxisData(_axis, _measIndex, RecordType.ActualPosition) -
                   _log.GetAxisData(_axis, _measIndex - 1, RecordType.ActualPosition);
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