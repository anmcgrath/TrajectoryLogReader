using System.Collections;

namespace TrajectoryLogReader.Log;

public class MeasurementDataCollection : IEnumerable<MeasurementData>
{
    private readonly TrajectoryLog _log;

    internal MeasurementDataCollection(TrajectoryLog log)
    {
        _log = log;
    }

    public IEnumerator<MeasurementData> GetEnumerator()
    {
        return new MeasurementDataEnumerator(_log);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}