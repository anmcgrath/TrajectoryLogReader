using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log;

/// <summary>
/// Represents the MLC leaf positions at a specific measurement point.
/// </summary>
public class MLCRecord
{
    private readonly int _measIndex;
    private readonly TrajectoryLog _log;

    internal MLCRecord(TrajectoryLog log, int measIndex)
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
    public float GetExpected(int leafIndex, int bankIndex)
    {
        return _log.GetMlcPosition(_measIndex, RecordType.ExpectedPosition, leafIndex, bankIndex);
    }

    /// <summary>
    /// Gets the actual position of a specific leaf.
    /// </summary>
    /// <param name="leafIndex">The index of the leaf.</param>
    /// <param name="bankIndex">The index of the bank (0 or 1).</param>
    /// <returns>The actual position in cm.</returns>
    public float GetActual(int leafIndex, int bankIndex)
    {
        return _log.GetMlcPosition(_measIndex, RecordType.ActualPosition, leafIndex, bankIndex);
    }

    /// <summary>
    /// Returns the difference (actual - expected) for the leaf at <paramref name="leafIndex"/> and bank <paramref name="bankIndex"/>
    /// </summary>
    /// <param name="bankIndex"></param>
    /// <param name="leafIndex"></param>
    /// <returns></returns>
    public float Delta(int bankIndex, int leafIndex)
    {
        return GetActual(leafIndex, bankIndex) - GetExpected(leafIndex, bankIndex);
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
        if (recordType == RecordType.ActualPosition)
            return _log.GetMlcPosition(_measIndex, RecordType.ActualPosition, leafIndex, bankIndex);
        else
            return _log.GetMlcPosition(_measIndex, RecordType.ExpectedPosition, leafIndex, bankIndex);
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
}