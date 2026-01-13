using System.Collections;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Fluence.Adapters;

internal class MeasurementDataCollectionAdapter : IFieldDataCollection
{
    private readonly SnapshotCollection _dataCollection;
    private readonly RecordType _recordType;
    private readonly double _sampleRate;

    public MeasurementDataCollectionAdapter(SnapshotCollection dataCollection,
        RecordType recordType, double sampleRate)
    {
        _dataCollection = dataCollection;
        _recordType = recordType;
        _sampleRate = sampleRate;
    }

    public IEnumerator<IFieldData> GetEnumerator()
    {
        return GetFieldData().GetEnumerator();
    }

    private IEnumerable<IFieldData> GetFieldData()
    {
        var prevMu = _dataCollection.First().MU.GetRecord(_recordType);
        foreach (var d in _dataCollection)
        {
            if (d.TimeInMs % _sampleRate != 0)
                continue;

            yield return new SnapshotDataAdapter(d, _recordType, prevMu);
            prevMu = d.MU.GetRecord(_recordType);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}