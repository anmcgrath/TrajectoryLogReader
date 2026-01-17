using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Snapshots;
using TrajectoryLogReader.MLC;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Fluence.Adapters;

internal class SnapshotDataAdapter : IFieldData
{
    private readonly Snapshot _data;
    private readonly RecordType _recordType;
    private readonly float _prevMu;

    public SnapshotDataAdapter(Snapshot data, RecordType recordType, float prevMu)
    {
        _data = data;
        _recordType = recordType;
        _prevMu = prevMu;
    }

    public IMLCModel Mlc => _data.MlcModel;
    public float X1InMm => _data.X1.WithScale(AxisScale.IEC61217).GetRecord(_recordType) * 10;
    public float Y1InMm => _data.Y1.WithScale(AxisScale.IEC61217).GetRecord(_recordType) * 10;
    public float X2InMm => _data.X2.WithScale(AxisScale.IEC61217).GetRecord(_recordType) * 10;
    public float Y2InMm => _data.Y2.WithScale(AxisScale.IEC61217).GetRecord(_recordType) * 10;
    public float GantryInDegrees => _data.GantryRtn.WithScale(AxisScale.IEC61217).GetRecord(_recordType);
    public float CollimatorInDegrees => _data.CollRtn.WithScale(AxisScale.IEC61217).GetRecord(_recordType);

    public float GetLeafPositionInMm(int bank, int leafIndex)
    {
        return _data.MLC.WithScale(AxisScale.IEC61217).GetLeaf(bank, leafIndex).GetRecord(_recordType) * 10;
    }

    public float DeltaMu => _data.MU.GetRecord(_recordType) - _prevMu;
}