using TrajectoryLogReader.DICOM.Plan;
using TrajectoryLogReader.Fluence;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.DICOM.FluenceAdapters;

/// <summary>
/// Adapts a single control point and MU to IFieldData.
/// </summary>
public class BeamFieldDataAdapter : IFieldData
{
    private ControlPointData _cp;
    private readonly BeamModel _beam;

    public BeamFieldDataAdapter(ControlPointData cp, float deltaMu, BeamModel beam)
    {
        _cp = cp;
        DeltaMu = deltaMu;
        _beam = beam;
    }

    public IMLCModel Mlc => _beam.Mlc; // TODO create one based on leaf boundaries
    public float X1InMm => _cp.X1 ?? 0;
    public float Y1InMm => _cp.Y1 ?? 0;
    public float X2InMm => _cp.X2 ?? 0;
    public float Y2InMm => _cp.Y2 ?? 0;

    public float GantryInDegrees => _cp.GantryAngle ?? 0;
    public float CollimatorInDegrees => _cp.CollimatorAngle ?? 0;

    public float GetLeafPositionInMm(int bank, int leafIndex)
    {
        if (_cp.MlcData == null)
            throw new Exception($"Invalid MLC data");

        return _cp.MlcData[1 - bank, leafIndex];
    }

    public float DeltaMu { get; }

    public bool IsBeamHold() => false;
}