using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

/// <summary>
/// Converts between Varian native scale and other scales.
/// </summary>
public class VarianNativeScaleConverter : IScaleConverter
{
    /// <summary>
    /// Converts a value to the specified scale.
    /// </summary>
    public float Convert(AxisScale to, Axis axis, float value)
    {
        switch (to)
        {
            case AxisScale.MachineScale:
                return value;
            case AxisScale.MachineScaleIsocentric:
                return value;
            case AxisScale.ModifiedIEC61217:
                return ToIec(axis, value);
        }

        throw new ScaleConversionException(AxisScale.ModifiedIEC61217, to, axis);
    }

    /// <inheritdoc />
    public float ToIec(Axis axis, float value)
    {
        switch (axis)
        {
            case Axis.GantryRtn:
            case Axis.CouchRtn:
            case Axis.CollRtn:
                return ((180 - value) % 360 + 360) % 360;
            case Axis.CouchVrt:
            case Axis.CouchLat:
                return -(100 - value);
            case Axis.X1:
                return -value;
            case Axis.Y1:
                return -value;
        }

        return value;
    }

    /// <inheritdoc />
    public float FromIec(Axis axis, float value)
    {
        switch (axis)
        {
            case Axis.GantryRtn:
            case Axis.CouchRtn:
            case Axis.CollRtn:
                return ((180 - value) % 360 + 360) % 360;
            case Axis.CouchVrt:
            case Axis.CouchLat:
                return -(100 - value);
            case Axis.X1:
                return -value;
            case Axis.Y1:
                return -value;
        }

        return value;
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