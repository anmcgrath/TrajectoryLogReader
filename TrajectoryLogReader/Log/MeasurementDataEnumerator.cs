using System.Collections;

namespace TrajectoryLogReader.Log;

internal class MeasurementDataEnumerator : IEnumerator<MeasurementData>
{
    private readonly TrajectoryLog _log;
    private int _measurementIndex = -1;

    internal MeasurementDataEnumerator(TrajectoryLog log)
    {
        _log = log;
    }

    public bool MoveNext()
    {
        _measurementIndex++;
        return _measurementIndex < _log.Header.NumberOfSnapshots;
    }

    public void Reset()
    {
        _measurementIndex = -1;
    }

    public MeasurementData Current => new MeasurementData(_measurementIndex, _log);

    object IEnumerator.Current => Current;

    public void Dispose()
    {
    }
}