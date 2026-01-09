using System.Collections;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.DICOM.FluenceAdapters;

public class BeamCollectionAdapter : IFieldDataCollection
{
    public BeamModel _beam;

    public BeamCollectionAdapter(BeamModel beam)
    {
        _beam = beam;
    }

    public IEnumerator<IFieldData> GetEnumerator()
    {
        return GetFieldData().GetEnumerator();
    }

    public IEnumerable<IFieldData> GetFieldData()
    {
        float prevMu = 0;
        foreach (var cp in _beam.ControlPoints)
        {
            var mu = cp.CumulativeMetersetWeight * _beam.MU;
            yield return new BeamFieldDataAdapter(cp, mu - prevMu, _beam);
            prevMu = mu;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}