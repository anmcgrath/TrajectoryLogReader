using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log;

/// <summary>
/// Represents a single scalar value record (e.g., Gantry angle, Couch position) from the log.
/// </summary>
public class ScalarRecord
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

    /// <summary>
    /// Creates a new ScalarRecord with values converted to the specified scale.
    /// </summary>
    /// <param name="scale">The target scale for value conversion.</param>
    /// <returns>A new ScalarRecord configured to return values in the specified scale.</returns>
    public ScalarRecord WithScale(AxisScale scale)
    {
        return new ScalarRecord(_log, _axis, _measIndex, scale);
    }

    // Raw values in native log scale (used internally)
    private float RawExpected => _log.GetAxisData(_axis, _measIndex, RecordType.ExpectedPosition);
    private float RawActual => _log.GetAxisData(_axis, _measIndex, RecordType.ActualPosition);
    private AxisScale SourceScale => _log.Header.AxisScale;
    private AxisScale EffectiveScale => _targetScale ?? SourceScale;

    /// <summary>
    /// The expected position of the axis (in target scale if WithScale was called, otherwise native scale).
    /// </summary>
    public float Expected => _targetScale.HasValue
        ? Scale.Convert(SourceScale, _targetScale.Value, _axis, RawExpected)
        : RawExpected;

    /// <summary>
    /// The actual position of the axis (in target scale if WithScale was called, otherwise native scale).
    /// </summary>
    public float Actual => _targetScale.HasValue
        ? Scale.Convert(SourceScale, _targetScale.Value, _axis, RawActual)
        : RawActual;

    /// <summary>
    /// Returns the scalar record of type <paramref name="type"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public float GetRecord(RecordType type)
    {
        return type == RecordType.ExpectedPosition ? Expected : Actual;
    }

    /// <summary>
    /// Actual - Expected (computed in effective scale with proper normalization for rotational axes)
    /// </summary>
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