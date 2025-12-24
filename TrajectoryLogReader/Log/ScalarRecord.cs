using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log;

public class ScalarRecord
{
    private readonly int _measIndex;
    private readonly TrajectoryLog _log;
    private readonly Axis _axis;

    internal ScalarRecord(TrajectoryLog log, Axis axis, int measIndex)
    {
        _measIndex = measIndex;
        _log = log;
        _axis = axis;
    }

    public float Expected => _log.GetAxisData(_axis, _measIndex, RecordType.ExpectedPosition);

    public float Actual => _log.GetAxisData(_axis, _measIndex, RecordType.ActualPosition);

    /// <summary>
    /// Actual - Expected
    /// </summary>
    public float Delta => Scale.Delta(_log.Header.AxisScale, Expected, _log.Header.AxisScale, Actual, _axis);

    /// <summary>
    /// Returns the expected value in the coord system/scale <paramref name="axisScale"/>
    /// </summary>
    /// <param name="axisScale"></param>
    /// <returns></returns>
    public float ExpectedInScale(AxisScale axisScale) =>
        Scale.Convert(_log.Header.AxisScale, axisScale, _axis, Expected);

    /// <summary>
    /// Returns the actual value in the coord system/scale <paramref name="axisScale"/>
    /// </summary>
    /// <param name="axisScale"></param>
    /// <returns></returns>
    public float ActualInScale(AxisScale axisScale) =>
        Scale.Convert(_log.Header.AxisScale, axisScale, _axis, Actual);

    /// <summary>
    /// Returns the expected value in IEC scale
    /// </summary>
    /// <returns></returns>
    public float ExpectedInIec() =>
        Scale.ToIec(_log.Header.AxisScale, _axis, Expected);

    /// <summary>
    /// Returns the actual value in IEC scale
    /// </summary>
    /// <returns></returns>
    public float ActualInIec() =>
        Scale.ToIec(_log.Header.AxisScale, _axis, Actual);
}