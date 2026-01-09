using TrajectoryLogReader.Log;
using TrajectoryLogReader.MLC;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Fluence.Adapters;

internal class MeasurementDataAdapter : IFieldData
{
    private MeasurementData _data;
    private RecordType _recordType;
    private readonly float _prevMu;

    public MeasurementDataAdapter(MeasurementData data, RecordType recordType, float prevMu)
    {
        _data = data;
        _recordType = recordType;
        _prevMu = prevMu;
    }

    public IMLCModel Mlc => _data.MlcModel;
    public float X1InCm => _data.GetScalarRecord(Axis.X1).GetRecordInIec(_recordType);
    public float Y1InCm => _data.GetScalarRecord(Axis.Y1).GetRecordInIec(_recordType);
    public float X2InCm => _data.GetScalarRecord(Axis.X2).GetRecordInIec(_recordType);
    public float Y2InCm => _data.GetScalarRecord(Axis.Y2).GetRecordInIec(_recordType);
    public float GantryInDegrees => _data.GetScalarRecord(Axis.GantryRtn).GetRecordInIec(_recordType);
    public float CollimatorInDegrees => _data.GetScalarRecord(Axis.CollRtn).GetRecordInIec(_recordType);

    public float[,] MlcPositionsInCm =>
        _recordType == RecordType.ActualPosition ? _data.MLC.Actual : _data.MLC.Expected;


    public float GetLeafPosition(int bank, int leafIndex)
    {
        return _data.MLC.GetRecordInIec(bank, leafIndex, _recordType);
    }

    public float DeltaMu => _data.GetScalarRecord(Axis.MU).GetRecord(_recordType) - _prevMu;
}