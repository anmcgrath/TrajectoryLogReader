using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

/// <summary>
/// Converts from the VarianIEC coords to Machine scales
/// </summary>
public class VarianIECScaleConverter : IScaleConverter
{
    /// <inheritdoc />
    public float ToIec(Axis axis, float value)
    {
        switch (axis)
        {
            case Axis.CouchRtn:
                return (360 - value) % 360;
            case Axis.CouchVrt:
                return value >= 1000 ? value - 1000 : -(value % 1000);
            case Axis.CouchLat:
                return value <= 500 ? value : value - 1000;
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
            case Axis.CouchRtn:
                return (360 - value) % 360;
            case Axis.CouchVrt:
                return value >= 0 ? 1000 + value : -value;
            case Axis.CouchLat:
                return value >= 0 ? value : 1000 + value;
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