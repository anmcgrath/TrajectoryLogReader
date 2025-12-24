using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

public class VarianNativeScaleConverter : IScaleConverter
{
    public float Convert(AxisScale to, Axis axis, float value)
    {
        switch (to)
        {
            case AxisScale.MachineScale:
                return value;
            case AxisScale.MachineScaleIsocentric:
                return value;
            case AxisScale.ModifiedIEC61217:
                switch (axis)
                {
                    case Axis.GantryRtn:
                    case Axis.CouchRtn:
                    case Axis.CollRtn:
                        return ((180 - value) % 360 + 360) % 360;
                    default:
                        return value;
                }
        }

        throw new ScaleConversionException(AxisScale.ModifiedIEC61217, to, axis);
    }

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
        }

        return value;
    }

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
        }

        return value;
    }
}