using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

/// <summary>
/// Exception thrown when a scale conversion fails.
/// </summary>
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