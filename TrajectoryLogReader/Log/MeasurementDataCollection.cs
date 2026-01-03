using System.Collections;

namespace TrajectoryLogReader.Log;

public class MeasurementDataCollection : IEnumerable<MeasurementData>
{
    private readonly TrajectoryLog _log;
    private readonly int _startIndex;
    private readonly int _endIndex;

    public int Count => _endIndex < 0 ? 0 : (_endIndex - _startIndex + 1);

    internal MeasurementDataCollection(TrajectoryLog log, int startIndex, int endIndex)
    {
        _log = log;
        _startIndex = startIndex;
        _endIndex = endIndex;
    }

    public IEnumerator<MeasurementData> GetEnumerator()
    {
        var list = new List<MeasurementData>();
        list.First();
        return new MeasurementDataEnumerator(_log, _startIndex, _endIndex);
    }

    public MeasurementData Last()
    {
        if (Count == 0)
            throw new InvalidOperationException();
        return new MeasurementData(_endIndex, _log);
    }

    public MeasurementData First()
    {
        if (Count == 0)
            throw new InvalidOperationException();
        return new MeasurementData(_startIndex, _log);
    }

    public MeasurementData LastOrDefault()
    {
        return Count == 0 ? null : Last();
    }

    public MeasurementData FirstOrDefault()
    {
        return Count == 0 ? null : First();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}