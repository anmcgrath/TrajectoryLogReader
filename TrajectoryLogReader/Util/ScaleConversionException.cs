using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

public class ScaleConversionException : Exception
{
    public AxisScale From { get; }
    public AxisScale To { get; }
    public Axis Axis { get; }

    public ScaleConversionException(AxisScale from, AxisScale to, Axis axis) :
        base($"No scale conversion exists {from} to {to} for axis {axis}")
    {
        From = from;
        To = to;
        Axis = axis;
    }
}