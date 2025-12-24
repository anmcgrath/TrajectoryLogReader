namespace TrajectoryLogReader.Log;

public class MLCRecord
{
    private readonly int _measIndex;
    private readonly TrajectoryLog _log;

    internal MLCRecord(TrajectoryLog log, int measIndex)
    {
        _measIndex = measIndex;
        _log = log;
    }

    public float[,] Expected => _log.GetMlcPositions(_measIndex, RecordType.ExpectedPosition);

    public float[,] Actual => _log.GetMlcPositions(_measIndex, RecordType.ActualPosition);

    public float GetExpected(int leafIndex, int bankIndex)
    {
        return _log.GetMlcPosition(_measIndex, RecordType.ExpectedPosition, leafIndex, bankIndex);
    }

    public float GetActual(int leafIndex, int bankIndex)
    {
        return _log.GetMlcPosition(_measIndex, RecordType.ActualPosition, leafIndex, bankIndex);
    }

    /// <summary>
    /// Returns the difference (actual - expected) for the leaf at <paramref name="leafIndex"/> and bank <paramref name="bankIndex"/>
    /// </summary>
    /// <param name="leafIndex"></param>
    /// <param name="bankIndex"></param>
    /// <returns></returns>
    public float Delta(int leafIndex, int bankIndex)
    {
        return GetActual(leafIndex, bankIndex) - GetExpected(leafIndex, bankIndex);
    }
}