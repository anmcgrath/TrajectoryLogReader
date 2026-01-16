using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

/// <summary>
/// Converts between Varian native isocentric scale and other scales.
/// </summary>
public class VarianNativeIsocentricConverter : IScaleConverter
{
    private readonly VarianNativeScaleConverter _varianNativeScaleConverter = new();

    /// <summary>
    /// Converts a value to the specified scale.
    /// </summary>
    public float Convert(AxisScale to, Axis axis, float value)
    {
        return _varianNativeScaleConverter.Convert(to, axis, value);
    }

    /// <inheritdoc />
    public float ToIec(Axis axis, float value)
    {
        return _varianNativeScaleConverter.ToIec(axis, value);
    }

    /// <inheritdoc />
    public float FromIec(Axis axis, float value)
    {
        return _varianNativeScaleConverter.FromIec(axis, value);
    }

    /// <inheritdoc />
    public float MlcPositionToIec(int bank, float value)
    {
        if (bank == 1)
            return -value;
        return value;
    }

    /// <inheritdoc />
    public float MlcPositionFromIec(int bank, float value)
    {
        if (bank == 1)
            return -value;
        return value;
    }
}