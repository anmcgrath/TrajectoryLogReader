using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Snapshots;

/// <summary>
/// Represents a single scalar value record (e.g., Gantry angle, Couch position) from the log.
/// </summary>
public class ScalarRecord : IScalarRecord
{
    private readonly int _measIndex;
    private readonly TrajectoryLog _log;
    private readonly Axis _axis;
    private readonly AxisScale? _targetScale;

    internal ScalarRecord(TrajectoryLog log, Axis axis, int measIndex, AxisScale? targetScale = null)
    {
        _measIndex = measIndex;
        _log = log;
        _axis = axis;
        _targetScale = targetScale;
    }

    public IScalarRecord WithScale(AxisScale scale)
    {
        return new ScalarRecord(_log, _axis, _measIndex, scale);
    }

    // Raw values in native log scale (used internally)
    private float RawExpected => _log.GetAxisData(_axis, _measIndex, RecordType.ExpectedPosition);
    private float RawActual => _log.GetAxisData(_axis, _measIndex, RecordType.ActualPosition);
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

    /// <summary>
    /// Returns the expected value in the coord system/scale <paramref name="axisScale"/>
    /// </summary>
    /// <param name="axisScale"></param>
    /// <returns></returns>
    [Obsolete("Use WithScale(axisScale).Expected instead")]
    public float ExpectedInScale(AxisScale axisScale) =>
        Scale.Convert(SourceScale, axisScale, _axis, RawExpected);

    /// <summary>
    /// Returns the actual value in the coord system/scale <paramref name="axisScale"/>
    /// </summary>
    /// <param name="axisScale"></param>
    /// <returns></returns>
    [Obsolete("Use WithScale(axisScale).Actual instead")]
    public float ActualInScale(AxisScale axisScale) =>
        Scale.Convert(SourceScale, axisScale, _axis, RawActual);

    /// <summary>
    /// Returns the expected value in IEC scale
    /// </summary>
    /// <returns></returns>
    [Obsolete("Use WithScale(AxisScale.IEC61217).Expected instead")]
    public float ExpectedInIec() =>
        Scale.ToIec(SourceScale, _axis, RawExpected);

    /// <summary>
    /// Returns the actual value in IEC scale
    /// </summary>
    /// <returns></returns>
    [Obsolete("Use WithScale(AxisScale.IEC61217).Actual instead")]
    public float ActualInIec() =>
        Scale.ToIec(SourceScale, _axis, RawActual);

    /// <summary>
    /// Returns the record value in IEC scale.
    /// </summary>
    /// <param name="recordType">The type of record (Expected or Actual).</param>
    /// <returns>The value in IEC scale.</returns>
    [Obsolete("Use WithScale(AxisScale.IEC61217).GetRecord(recordType) instead")]
    public float GetRecordInIec(RecordType recordType) =>
        Scale.ToIec(SourceScale, _axis, recordType == RecordType.ExpectedPosition ? RawExpected : RawActual);
}