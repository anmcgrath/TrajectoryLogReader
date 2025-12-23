using System.Collections;

namespace TrajectoryLogReader.Log;

public class MeasurementDataCollection : IEnumerable<MeasurementData>
{
    private readonly TrajectoryLog _log;
    private readonly int _startIndex;
    private readonly int _endIndex;

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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}