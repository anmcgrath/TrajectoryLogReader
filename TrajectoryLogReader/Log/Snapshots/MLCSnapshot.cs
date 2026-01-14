using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Snapshots;

/// <summary>
/// Represents the MLC leaf positions at a specific measurement point.
/// </summary>
public class MLCSnapshot
{
    private readonly int _measIndex;
    private readonly TrajectoryLog _log;

    internal MLCSnapshot(TrajectoryLog log, int measIndex)
    {
        _measIndex = measIndex;
        _log = log;
    }

    /// <summary>
    /// The expected positions of all MLC leaves.
    /// </summary>
    public float[,] Expected => _log.GetMlcPositions(_measIndex, RecordType.ExpectedPosition);

    /// <summary>
    /// The actual positions of all MLC leaves.
    /// </summary>
    public float[,] Actual => _log.GetMlcPositions(_measIndex, RecordType.ActualPosition);

    /// <summary>
    /// Gets the expected position of a specific leaf.
    /// </summary>
    /// <param name="leafIndex">The index of the leaf.</param>
    /// <param name="bankIndex">The index of the bank (0 or 1).</param>
    /// <returns>The expected position in cm.</returns>
    public float GetExpected(int bankIndex, int leafIndex) =>
        GetRecord(bankIndex, leafIndex, RecordType.ExpectedPosition);

    /// <summary>
    /// Gets the actual position of a specific leaf.
    /// </summary>
    /// <param name="leafIndex">The index of the leaf.</param>
    /// <param name="bankIndex">The index of the bank (0 or 1).</param>
    /// <returns>The actual position in cm.</returns>
    public float GetActual(int bankIndex, int leafIndex) =>
        GetRecord(bankIndex, leafIndex, RecordType.ActualPosition);

    /// <summary>
    /// Returns the difference (actual - expected) for the leaf at <paramref name="leafIndex"/> and bank <paramref name="bankIndex"/>
    /// </summary>
    /// <param name="bankIndex"></param>
    /// <param name="leafIndex"></param>
    /// <returns></returns>
    public float GetDelta(int bankIndex, int leafIndex)
    {
        return GetActual(bankIndex, leafIndex) - GetExpected(bankIndex, leafIndex);
    }

    /// <summary>
    /// Gets a specific record type for a leaf.
    /// </summary>
    /// <param name="bankIndex">The bank index.</param>
    /// <param name="leafIndex">The leaf index.</param>
    /// <param name="recordType">The type of record (Expected or Actual).</param>
    /// <returns>The position.</returns>
    public float GetRecord(int bankIndex, int leafIndex, RecordType recordType)
    {
        return _log.GetMlcPosition(_measIndex, recordType, leafIndex, bankIndex);
    }

    /// <summary>
    /// Gets a specific record type for a leaf in IEC scale.
    /// </summary>
    /// <param name="bankIndex">The bank index.</param>
    /// <param name="leafIndex">The leaf index.</param>
    /// <param name="recordType">The type of record (Expected or Actual).</param>
    /// <returns>The position in IEC scale.</returns>
    public float GetRecordInIec(int bankIndex, int leafIndex, RecordType recordType)
    {
        return Scale.MlcToIec(_log.Header.AxisScale, bankIndex, GetRecord(bankIndex, leafIndex, recordType));
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

        var p0 = Scale.MlcToIec(_log.Header.AxisScale, bankIndex,
            _log.GetMlcPosition(_measIndex - 1, recordType, leafIndex, bankIndex));
        var p1 = Scale.MlcToIec(_log.Header.AxisScale, bankIndex,
            _log.GetMlcPosition(_measIndex, recordType, leafIndex, bankIndex));

        return (p1 - p0) / _log.Header.SamplingIntervalInMS;
    }
}