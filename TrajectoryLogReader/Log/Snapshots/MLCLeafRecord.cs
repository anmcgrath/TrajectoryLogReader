using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Snapshots;

/// <summary>
/// Represents a single MLC leaf position record at a specific measurement point.
/// Provides a unified API consistent with ScalarRecord and the column-based MlcLeafAxisAccessor.
/// </summary>
public class MLCLeafRecord : IScalarRecord
{
    private readonly int _measIndex;
    private readonly TrajectoryLog _log;
    private readonly int _bankIndex;
    private readonly int _leafIndex;
    private readonly AxisScale? _targetScale;

    internal MLCLeafRecord(TrajectoryLog log, int measIndex, int bankIndex, int leafIndex,
        AxisScale? targetScale = null)
    {
        _log = log;
        _measIndex = measIndex;
        _bankIndex = bankIndex;
        _leafIndex = leafIndex;
        _targetScale = targetScale;
    }

    /// <summary>
    /// The bank index (0 or 1) of this leaf.
    /// </summary>
    public int BankIndex => _bankIndex;

    /// <summary>
    /// The leaf index within the bank.
    /// </summary>
    public int LeafIndex => _leafIndex;

    /// <summary>
    /// Creates a new MLCLeafRecord with values converted to the specified scale.
    /// </summary>
    /// <param name="scale">The target scale for value conversion.</param>
    /// <returns>A new MLCLeafRecord configured to return values in the specified scale.</returns>
    public IScalarRecord WithScale(AxisScale scale)
    {
        return new MLCLeafRecord(_log, _measIndex, _bankIndex, _leafIndex, scale);
    }

    // Raw values in native log scale (used internally)
    private float RawExpected => _log.GetMlcPosition(_measIndex, RecordType.ExpectedPosition, _leafIndex, _bankIndex);
    private float RawActual => _log.GetMlcPosition(_measIndex, RecordType.ActualPosition, _leafIndex, _bankIndex);
    private AxisScale SourceScale => _log.Header.AxisScale;

    /// <summary>
    /// The expected position of the leaf (in target scale if WithScale was called, otherwise native scale).
    /// </summary>
    public float Expected => _targetScale.HasValue
        ? Scale.ConvertMlc(SourceScale, _targetScale.Value, _bankIndex, RawExpected)
        : RawExpected;

    /// <summary>
    /// The actual position of the leaf (in target scale if WithScale was called, otherwise native scale).
    /// </summary>
    public float Actual => _targetScale.HasValue
        ? Scale.ConvertMlc(SourceScale, _targetScale.Value, _bankIndex, RawActual)
        : RawActual;

    /// <summary>
    /// The error (Actual - Expected) for this leaf.
    /// </summary>
    public float Error => Actual - Expected;

    /// <summary>
    /// Returns the record value for the specified record type.
    /// </summary>
    /// <param name="recordType">The type of record (Expected or Actual).</param>
    /// <returns>The position value.</returns>
    public float GetRecord(RecordType recordType)
    {
        return recordType == RecordType.ExpectedPosition ? Expected : Actual;
    }

    /// <summary>
    /// Gets the delta (change) from the previous snapshot to this one. Can be chained
    /// for higher-order derivatives (e.g., <c>GetDelta().GetDelta()</c> for acceleration).
    /// </summary>
    /// <param name="timeSpan">If specified, converts delta to a rate (e.g., deg/s, MU/min)</param>
    /// <returns>A delta record representing the change from previous to current</returns>
    public IScalarRecord GetDelta(TimeSpan? timeSpan = null)
    {
        var previous = _measIndex > 0
            ? new MLCLeafRecord(_log, _measIndex - 1, _bankIndex, _leafIndex, _targetScale)
            : null;
        var msConverter = timeSpan.HasValue
            ? (float)(timeSpan.Value.TotalMilliseconds / _log.Header.SamplingIntervalInMS)
            : 1f;
        return new DeltaRecord(previous, this, msConverter, Axis.MLC, _targetScale, _log);
    }
}