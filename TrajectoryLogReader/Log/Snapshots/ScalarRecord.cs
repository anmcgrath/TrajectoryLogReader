using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Snapshots;

/// <summary>
/// Provides access to a single scalar axis (for example gantry angle, couch position, or MU)
/// at one snapshot, with optional conversion into a clinically meaningful coordinate system.
/// Use <see cref="WithScale"/> to ensure comparisons and tolerances are evaluated in the
/// same frame of reference (for example IEC 61217).
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

    /// <summary>
    /// Returns a view of this record converted into the requested coordinate system.
    /// This is especially important when mixing machine-native logs with IEC-based
    /// plan data or clinical tolerances.
    /// </summary>
    /// <param name="scale">The coordinate system to express values in.</param>
    /// <returns>A scaled record that converts on access.</returns>
    public IScalarRecord WithScale(AxisScale scale) =>
        new ScalarRecord(_log, _axis, _measIndex, scale);

    // Raw values in native log scale (used internally)
    private float RawExpected => _log.GetAxisData(_axis, _measIndex, RecordType.ExpectedPosition);
    private float RawActual => _log.GetAxisData(_axis, _measIndex, RecordType.ActualPosition);
    private AxisScale SourceScale => _log.Header.AxisScale;
    private AxisScale EffectiveScale => _targetScale ?? SourceScale;

    /// <summary>
    /// The expected (commanded) axis value at this snapshot, expressed in the effective scale.
    /// </summary>
    public float Expected => _targetScale.HasValue
        ? Scale.Convert(SourceScale, _targetScale.Value, _axis, RawExpected)
        : RawExpected;

    /// <summary>
    /// The actual (measured) axis value at this snapshot, expressed in the effective scale.
    /// </summary>
    public float Actual => _targetScale.HasValue
        ? Scale.Convert(SourceScale, _targetScale.Value, _axis, RawActual)
        : RawActual;

    /// <summary>
    /// Returns either the expected or actual value using the same scale rules as
    /// <see cref="Expected"/> and <see cref="Actual"/>.
    /// </summary>
    /// <param name="type">Whether to return the commanded or measured value.</param>
    /// <returns>The requested value in the effective scale.</returns>
    public float GetRecord(RecordType type)
    {
        return type == RecordType.ExpectedPosition ? Expected : Actual;
    }

    /// <summary>
    /// The signed difference between expected and actual values, computed in a single
    /// coordinate system to avoid sign-convention mistakes.
    /// </summary>
    public float Error => Scale.Delta(EffectiveScale, Expected, EffectiveScale, Actual, _axis);

    /// <summary>
    /// Legacy helper that converts the expected value into the requested coordinate system.
    /// Prefer <c>WithScale(axisScale).Expected</c> for clearer intent.
    /// </summary>
    /// <param name="axisScale">The coordinate system to convert into.</param>
    /// <returns>The expected value expressed in <paramref name="axisScale"/>.</returns>
    [Obsolete("Use WithScale(axisScale).Expected instead")]
    public float ExpectedInScale(AxisScale axisScale) =>
        Scale.Convert(SourceScale, axisScale, _axis, RawExpected);

    /// <summary>
    /// Legacy helper that converts the actual value into the requested coordinate system.
    /// Prefer <c>WithScale(axisScale).Actual</c> for clearer intent.
    /// </summary>
    /// <param name="axisScale">The coordinate system to convert into.</param>
    /// <returns>The actual value expressed in <paramref name="axisScale"/>.</returns>
    [Obsolete("Use WithScale(axisScale).Actual instead")]
    public float ActualInScale(AxisScale axisScale) =>
        Scale.Convert(SourceScale, axisScale, _axis, RawActual);

    /// <summary>
    /// Legacy helper that returns the expected value in IEC 61217.
    /// Prefer <c>WithScale(AxisScale.IEC61217).Expected</c>.
    /// </summary>
    /// <returns>The expected value in IEC 61217 coordinates.</returns>
    [Obsolete("Use WithScale(AxisScale.IEC61217).Expected instead")]
    public float ExpectedInIec() =>
        Scale.ToIec(SourceScale, _axis, RawExpected);

    /// <summary>
    /// Legacy helper that returns the actual value in IEC 61217.
    /// Prefer <c>WithScale(AxisScale.IEC61217).Actual</c>.
    /// </summary>
    /// <returns>The actual value in IEC 61217 coordinates.</returns>
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

    /// <summary>
    /// Gets the delta (change) from the previous snapshot to this one. Can be chained
    /// for higher-order derivatives (e.g., <c>GetDelta().GetDelta()</c> for acceleration).
    /// </summary>
    /// <param name="timeSpan">If specified, converts delta to a rate (e.g., deg/s, MU/min)</param>
    /// <returns>A delta record representing the change from previous to current</returns>
    public IScalarRecord GetDelta(TimeSpan? timeSpan = null)
    {
        var previous = _measIndex > 0
            ? new ScalarRecord(_log, _axis, _measIndex - 1, _targetScale)
            : null;
        var msConverter = timeSpan.HasValue
            ? (float)(timeSpan.Value.TotalMilliseconds / _log.Header.SamplingIntervalInMS)
            : 1f;
        return new DeltaRecord(previous, this, msConverter, _axis, true, _targetScale, _log);
    }
}
