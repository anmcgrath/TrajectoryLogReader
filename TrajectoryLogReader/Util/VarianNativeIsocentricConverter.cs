using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

public class VarianNativeIsocentricConverter : IScaleConverter
{
    private readonly VarianNativeScaleConverter _varianNativeScaleConverter = new();

    public float Convert(AxisScale to, Axis axis, float value)
    {
        return _varianNativeScaleConverter.Convert(to, axis, value);
    }

    public float ToIec(Axis axis, float value)
    {
        return _varianNativeScaleConverter.ToIec(axis, value);
    }

    public float FromIec(Axis axis, float value)
    {
        return _varianNativeScaleConverter.FromIec(axis, value);
    }

    public float MlcPositionToIec(int bank, float value)
    {
        return _varianNativeScaleConverter.MlcPositionToIec(bank, value);
    }

    public float MlcPositionFromIec(int bank, float value)
    {
        return _varianNativeScaleConverter.MlcPositionFromIec(bank, value);
    }
}