using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

/// <summary>
/// Utility class for handling axis scale conversions.
/// </summary>
public static class Scale
{
    private static Dictionary<AxisScale, IScaleConverter> _converters = new()
    {
        { AxisScale.MachineScale, new VarianNativeScaleConverter() },
        { AxisScale.ModifiedIEC61217, new VarianIECScaleConverter() },
        { AxisScale.MachineScaleIsocentric, new VarianNativeIsocentricConverter() }
    };

    /// <summary>
    /// Converts a value from one scale to another.
    /// </summary>
    public static float Convert(AxisScale from, AxisScale to, Axis axis, float value)
    {
        if (_converters.TryGetValue(from, out var fromConverter))
        {
            var iec = fromConverter.ToIec(axis, value);
            if (_converters.TryGetValue(to, out var toConverter))
                return toConverter.FromIec(axis, iec);
        }

        throw new ScaleConversionException(from, to, axis);
    }

    /// <summary>
    /// Converts an MLC value from one scale to another.
    /// </summary>
    public static float ConvertMlc(AxisScale from, AxisScale to, int bank, float value)
    {
        if (_converters.TryGetValue(from, out var fromConverter))
        {
            var iec = fromConverter.MlcPositionToIec(bank, value);
            if (_converters.TryGetValue(to, out var toConverter))
                return toConverter.MlcPositionFromIec(bank, iec);
        }

        throw new Exception($"Cannot convert MLC from {from} to {to}");
    }

    /// <summary>
    /// Converts a value to IEC scale.
    /// </summary>
    public static float ToIec(AxisScale from, Axis axis, float value)
    {
        if (_converters.TryGetValue(from, out var fromConverter))
        {
            return fromConverter.ToIec(axis, value);
        }

        throw new Exception($"Cannot convert {from} to IEC for axis {axis}");
    }

    /// <summary>
    /// Converts an MLC position to IEC scale.
    /// </summary>
    public static float MlcToIec(AxisScale from, int bank, float value)
    {
        if (_converters.TryGetValue(from, out var fromConverter))
        {
            return fromConverter.MlcPositionToIec(bank, value);
        }

        throw new Exception($"Cannot convert {from} to IEC for axis MLC");
    }

    /// <summary>
    /// Computes the smallest difference <paramref name="val2"/> - <paramref name="val2"/>
    /// </summary>
    /// <param name="scale1"></param>
    /// <param name="val1"></param>
    /// <param name="scale2"></param>
    /// <param name="val2"></param>
    /// <param name="axis">The axis we are calculating the difference for</param>
    /// <returns></returns>
    public static float Delta(AxisScale scale1, float val1, AxisScale scale2, float val2, Axis axis)
    {
        var val1Iec = Scale.ToIec(scale1, axis, val1);
        var val2Iec = Scale.ToIec(scale2, axis, val2);
        switch (axis)
        {
            case Axis.CollRtn:
            case Axis.GantryRtn:
            case Axis.CouchRtn:
                var diff = val2Iec - val1Iec;
                if (diff < -180)
                    return 360 + diff;
                if (diff >= 180)
                    return diff - 360;
                return diff;
            default: return val2Iec - val1Iec;
        }
    }
}