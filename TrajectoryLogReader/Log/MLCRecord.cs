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
}