using System.Collections;

namespace TrajectoryLogReader.Log;

public class MeasurementDataCollection : IEnumerable<MeasurementData>
{
    internal TrajectoryLog Log { get; }
    private readonly int _startIndex;
    private readonly int _endIndex;

    public int Count => _endIndex < 0 ? 0 : (_endIndex - _startIndex + 1);

    internal MeasurementDataCollection(TrajectoryLog log, int startIndex, int endIndex)
    {
        Log = log;
        _startIndex = startIndex;
        _endIndex = endIndex;
    }

    public IEnumerator<MeasurementData> GetEnumerator()
    {
        return new MeasurementDataEnumerator(Log, _startIndex, _endIndex);
    }

    public MeasurementData Last()
    {
        if (Count == 0)
            throw new InvalidOperationException();
        return new MeasurementData(_endIndex, Log);
    }

    public MeasurementData First()
    {
        if (Count == 0)
            throw new InvalidOperationException();
        return new MeasurementData(_startIndex, Log);
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