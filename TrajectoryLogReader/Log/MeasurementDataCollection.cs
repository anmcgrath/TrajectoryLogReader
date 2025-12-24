using System.Collections;

namespace TrajectoryLogReader.Log;

public class MeasurementDataCollection : IEnumerable<MeasurementData>
{
    private readonly TrajectoryLog _log;
    private readonly int _startIndex;
    private readonly int _endIndex;

    public int Count => (_endIndex - _startIndex + 1);

    internal MeasurementDataCollection(TrajectoryLog log, int startIndex, int endIndex)
    {
        _log = log;
        _startIndex = startIndex;
        _endIndex = endIndex;
    }

    public IEnumerator<MeasurementData> GetEnumerator()
    {
        return new MeasurementDataEnumerator(_log, _startIndex, _endIndex);
    }

    public MeasurementData Last()
    {
        return new MeasurementData(_endIndex, _log);
    }

    public MeasurementData First()
    {
        return new MeasurementData(_startIndex, _log);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}