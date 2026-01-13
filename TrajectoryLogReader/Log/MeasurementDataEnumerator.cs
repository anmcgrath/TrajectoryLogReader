using System.Collections;

namespace TrajectoryLogReader.Log;

internal class MeasurementDataEnumerator : IEnumerator<Snapshot>
{
    private readonly TrajectoryLog _log;
    private readonly int _startIndex;
    private readonly int _endIndex;
    private int _measurementIndex;

    internal MeasurementDataEnumerator(TrajectoryLog log, int startIndex, int endIndex)
    {
        _measurementIndex = startIndex - 1;
        _log = log;
        _startIndex = startIndex;
        _endIndex = endIndex;
    }

    public bool MoveNext()
    {
        _measurementIndex++;
        return _measurementIndex <= _endIndex && _endIndex >= 0;
    }

    public void Reset()
    {
        _measurementIndex = _startIndex - 1;
    }

    public Snapshot Current => new Snapshot(_measurementIndex, _log);

    object IEnumerator.Current => Current;

    public void Dispose()
    {
    }
}