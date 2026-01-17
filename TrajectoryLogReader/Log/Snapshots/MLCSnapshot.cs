using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Snapshots;

/// <summary>
/// Represents the MLC leaf positions at a specific measurement point.
/// </summary>
public class MLCSnapshot
{
    private readonly int _measIndex;
    private readonly TrajectoryLog _log;
    private readonly AxisScale? _targetScale;

    internal MLCSnapshot(TrajectoryLog log, int measIndex, AxisScale? targetScale = null)
    {
        _measIndex = measIndex;
        _log = log;
        _targetScale = targetScale;
    }

    /// <summary>
    /// Creates a new MLCSnapshot with values converted to the specified scale.
    /// </summary>
    /// <param name="scale">The target scale for value conversion.</param>
    /// <returns>A new MLCSnapshot configured to return values in the specified scale.</returns>
    public MLCSnapshot WithScale(AxisScale scale)
    {
        return new MLCSnapshot(_log, _measIndex, scale);
    }

    // Helper properties for scale conversion
    private AxisScale SourceScale => _log.Header.AxisScale;
    private AxisScale EffectiveScale => _targetScale ?? SourceScale;

    // Get raw value in native log scale (used internally)
    private float GetRawRecord(int bankIndex, int leafIndex, RecordType recordType) =>
        _log.GetMlcPosition(_measIndex, recordType, leafIndex, bankIndex);

    /// <summary>
    /// The expected positions of all MLC leaves (in target scale if WithScale was called, otherwise native scale).
    /// </summary>
    public float[,] Expected => GetPositions(RecordType.ExpectedPosition);

    /// <summary>
    /// The actual positions of all MLC leaves (in target scale if WithScale was called, otherwise native scale).
    /// </summary>
    public float[,] Actual => GetPositions(RecordType.ActualPosition);

    private float[,] GetPositions(RecordType recordType)
    {
        var raw = _log.GetMlcPositions(_measIndex, recordType);
        if (!_targetScale.HasValue)
            return raw;

        var result = new float[raw.GetLength(0), raw.GetLength(1)];
        for (int bank = 0; bank < raw.GetLength(0); bank++)
        {
            for (int leaf = 0; leaf < raw.GetLength(1); leaf++)
            {
                result[bank, leaf] = Scale.ConvertMlc(SourceScale, _targetScale.Value, bank, raw[bank, leaf]);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets the expected position of a specific leaf (in target scale if WithScale was called).
    /// </summary>
    /// <param name="bankIndex">The index of the bank (0 or 1).</param>
    /// <param name="leafIndex">The index of the leaf.</param>
    /// <returns>The expected position.</returns>
    [Obsolete("Use GetLeaf(bankIndex, leafIndex).Expected instead")]
    public float GetExpected(int bankIndex, int leafIndex) =>
        GetRecord(bankIndex, leafIndex, RecordType.ExpectedPosition);

    /// <summary>
    /// Gets the actual position of a specific leaf (in target scale if WithScale was called).
    /// </summary>
    /// <param name="bankIndex">The index of the bank (0 or 1).</param>
    /// <param name="leafIndex">The index of the leaf.</param>
    /// <returns>The actual position.</returns>
    [Obsolete("Use GetLeaf(bankIndex, leafIndex).Actual instead")]
    public float GetActual(int bankIndex, int leafIndex) =>
        GetRecord(bankIndex, leafIndex, RecordType.ActualPosition);

    /// <summary>
    /// Returns the difference (actual - expected) for the leaf at <paramref name="leafIndex"/> and bank <paramref name="bankIndex"/>
    /// </summary>
    /// <param name="bankIndex"></param>
    /// <param name="leafIndex"></param>
    /// <returns></returns>
    [Obsolete("Use GetLeaf(bankIndex, leafIndex).Error instead")]
    public float GetDelta(int bankIndex, int leafIndex)
    {
        return GetActual(bankIndex, leafIndex) - GetExpected(bankIndex, leafIndex);
    }

    /// <summary>
    /// Gets an MLCLeafRecord for a specific leaf, providing a unified API consistent with ScalarRecord.
    /// </summary>
    /// <param name="bankIndex">The index of the bank (0 or 1).</param>
    /// <param name="leafIndex">The index of the leaf.</param>
    /// <returns>An MLCLeafRecord for the specified leaf.</returns>
    public MLCLeafRecord GetLeaf(int bankIndex, int leafIndex)
    {
        return new MLCLeafRecord(_log, _measIndex, bankIndex, leafIndex, _targetScale);
    }

    /// <summary>
    /// Gets a specific record type for a leaf (in target scale if WithScale was called).
    /// </summary>
    /// <param name="bankIndex">The bank index.</param>
    /// <param name="leafIndex">The leaf index.</param>
    /// <param name="recordType">The type of record (Expected or Actual).</param>
    /// <returns>The position.</returns>
    [Obsolete("Use GetLeaf(bankIndex, leafIndex).GetRecord(recordType) instead")]
    public float GetRecord(int bankIndex, int leafIndex, RecordType recordType)
    {
        var raw = GetRawRecord(bankIndex, leafIndex, recordType);
        return _targetScale.HasValue
            ? Scale.ConvertMlc(SourceScale, _targetScale.Value, bankIndex, raw)
            : raw;
    }

    /// <summary>
    /// Gets a specific record type for a leaf in IEC scale.
    /// </summary>
    /// <param name="bankIndex">The bank index.</param>
    /// <param name="leafIndex">The leaf index.</param>
    /// <param name="recordType">The type of record (Expected or Actual).</param>
    /// <returns>The position in IEC scale.</returns>
    [Obsolete("Use WithScale(AxisScale.IEC61217).GetRecord(bankIndex, leafIndex, recordType) instead")]
    public float GetRecordInIec(int bankIndex, int leafIndex, RecordType recordType)
    {
        return Scale.MlcToIec(SourceScale, bankIndex, GetRawRecord(bankIndex, leafIndex, recordType));
    }

    /// <summary>
    /// Returns the actual leaf speed (with direction in IEC coord system)
    /// </summary>
    /// <param name="bankIndex"></param>
    /// <param name="leafIndex"></param>
    /// <returns></returns>
    public float GetActualSpeedIec(int bankIndex, int leafIndex) =>
        GetSpeedIec(bankIndex, leafIndex, RecordType.ActualPosition);

    /// <summary>
    /// Returns the expected leaf speed (with direction in IEC coord system)
    /// </summary>
    /// <param name="bankIndex"></param>
    /// <param name="leafIndex"></param>
    /// <returns></returns>
    public float GetExpectedSpeedIec(int bankIndex, int leafIndex) =>
        GetSpeedIec(bankIndex, leafIndex, RecordType.ExpectedPosition);

    /// <summary>
    /// Returns the leaf speed (with direction in IEC coords)
    /// </summary>
    /// <param name="bankIndex"></param>
    /// <param name="leafIndex"></param>
    /// <param name="recordType"></param>
    /// <returns></returns>
    public float GetSpeedIec(int bankIndex, int leafIndex, RecordType recordType)
    {
        if (_measIndex == 0)
            return 0;

        var p0 = Scale.MlcToIec(SourceScale, bankIndex,
            _log.GetMlcPosition(_measIndex - 1, recordType, leafIndex, bankIndex));
        var p1 = Scale.MlcToIec(SourceScale, bankIndex,
            _log.GetMlcPosition(_measIndex, recordType, leafIndex, bankIndex));

        return (p1 - p0) / _log.Header.SamplingIntervalInMS;
    }
}