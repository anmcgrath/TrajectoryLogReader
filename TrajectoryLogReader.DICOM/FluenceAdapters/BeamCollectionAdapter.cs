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
        if (_beam.NumberOfControlPoints == 0)
            yield break;

        float prevMu = 0;
        var cpFrac = 0d;
        var maxIndex = _beam.NumberOfControlPoints - 1;

        // Loop until we reach the end of the trajectory
        while (cpFrac < maxIndex)
        {
            int index = (int)cpFrac;
            
            // Ensure we don't go out of bounds (though while loop should prevent this)
            if (index >= maxIndex)
                break;

            var cp0 = _beam.ControlPoints[index];
            var cp1 = _beam.ControlPoints[index + 1];

            var cpInterp = ControlPointInterpolator.Interpolate(cp0, cp1, cpFrac);
            var mu = cpInterp.CumulativeMetersetWeight * _beam.MU;
            
            yield return new BeamFieldDataAdapter(cpInterp, mu - prevMu, _beam);
            
            prevMu = mu;
            cpFrac += _cpDelta;
        }

        // Include the final control point to ensure we account for the total dose
        var cpLast = _beam.ControlPoints[maxIndex];
        var muLast = cpLast.CumulativeMetersetWeight * _beam.MU;
        
        // Only yield if there is remaining MU or if it's the only point (to show static fields correctly)
        // However, for consistency, we always yield the final state to reach the total MU.
        yield return new BeamFieldDataAdapter(cpLast, muLast - prevMu, _beam);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}