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
    private readonly float _deltaMu;
    private readonly BeamModel _beam;

    public BeamFieldDataAdapter(ControlPointData cp, float deltaMu, BeamModel beam)
    {
        _cp = cp;
        _deltaMu = deltaMu;
        _beam = beam;
    }

    public IMLCModel Mlc => _beam.Mlc; // TODO create one based on leaf boundaries
    public float X1InMm => _cp.X1;
    public float Y1InMm => _cp.Y1;
    public float X2InMm => _cp.X2;
    public float Y2InMm => _cp.Y2;

    public float GantryInDegrees => _cp.GantryAngle;
    public float CollimatorInDegrees => _cp.CollimatorAngle;

    public float GetLeafPositionInMm(int bank, int leafIndex)
    {
        return _cp.MlcData[bank, leafIndex];
    }

    public float DeltaMu => _deltaMu;
    
    public bool IsBeamHold() => _deltaMu == 0; // no beam-holds in dicom
}