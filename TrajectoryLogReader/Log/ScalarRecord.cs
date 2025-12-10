namespace TrajectoryLogReader.Log;

public class ScalarRecord
{
    private readonly int _measIndex;
    private readonly TrajectoryLog _log;
    private readonly Axis _axis;

    internal ScalarRecord(TrajectoryLog log, Axis axis, int measIndex)
    {
        _measIndex = measIndex;
        _log = log;
        _axis = axis;
    }

    public float Expected => _log.GetAxisData(_axis, _measIndex, RecordType.ExpectedPosition);

    public float Actual => _log.GetAxisData(_axis, _measIndex, RecordType.ActualPosition);
}