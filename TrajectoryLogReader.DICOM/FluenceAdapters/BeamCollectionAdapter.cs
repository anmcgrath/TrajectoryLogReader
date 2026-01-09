using System.Collections;
using TrajectoryLogReader.DICOM.Plan;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.DICOM.FluenceAdapters;

/// <summary>
/// Adapts a BeamModel to an IFieldDataCollection.
/// </summary>
public class BeamCollectionAdapter : IFieldDataCollection
{
    public BeamModel _beam;
    private readonly double _cpDelta;

    /// <summary>
    /// Initializes a new instance of the <see cref="BeamCollectionAdapter"/> class.
    /// </summary>
    /// <param name="beam">The beam model.</param>
    /// <param name="cpDelta">The fraction of cp to progres each step</param>
    public BeamCollectionAdapter(BeamModel beam, double cpDelta)
    {
        _beam = beam;
        _cpDelta = cpDelta;
    }

    public IEnumerator<IFieldData> GetEnumerator()
    {
        return GetFieldData().GetEnumerator();
    }

    public IEnumerable<IFieldData> GetFieldData()
    {
        float prevMu = 0;
        for (int i = 0; i < _beam.NumberOfControlPoints - 1; i++)
        {
            double cpFrac = i;
            var cp = _beam.ControlPoints[i];
            var cp1 = _beam.ControlPoints[i + 1];
            while (cpFrac < i + 1)
            {
                var cpInterp = ControlPointInterpolator.Interpolate(cp, cp1, cpFrac);
                var mu = cpInterp.CumulativeMetersetWeight * _beam.MU;
                yield return new BeamFieldDataAdapter(cpInterp, mu - prevMu, _beam);
                prevMu = mu;
                cpFrac += _cpDelta;
            }
        }

        // include the last control point
        var cpLast = _beam.ControlPoints[_beam.NumberOfControlPoints - 1];
        var muLast = cpLast.CumulativeMetersetWeight * _beam.MU;
        yield return new BeamFieldDataAdapter(cpLast, muLast - prevMu, _beam);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}