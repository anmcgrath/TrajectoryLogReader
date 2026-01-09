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
    public float X1InCm => _cp.X1 / 10f;
    public float Y1InCm => _cp.Y1 / 10f;
    public float X2InCm => _cp.X2 / 10f;
    public float Y2InCm => _cp.Y2 / 10f;

    public float GantryInDegrees => _cp.GantryAngle;
    public float CollimatorInDegrees => _cp.CollimatorAngle;

    public float GetLeafPosition(int bank, int leafIndex)
    {
        return _cp.MlcData[bank, leafIndex] / 10;
    }

    public float DeltaMu => _deltaMu;
}