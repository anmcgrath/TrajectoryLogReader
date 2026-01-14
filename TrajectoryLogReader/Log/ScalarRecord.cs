using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log;

/// <summary>
/// Represents a single scalar value record (e.g., Gantry angle, Couch position) from the log.
/// </summary>
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

    /// <summary>
    /// The expected position of the axis.
    /// </summary>
    public float Expected => _log.GetAxisData(_axis, _measIndex, RecordType.ExpectedPosition);

    /// <summary>
    /// The actual position of the axis.
    /// </summary>
    public float Actual => _log.GetAxisData(_axis, _measIndex, RecordType.ActualPosition);

    /// <summary>
    /// Returns the scalar record of type <paramref name="type"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public float GetRecord(RecordType type)
    {
        return type == RecordType.ExpectedPosition ? Expected : Actual;
    }

    /// <summary>
    /// Actual - Expected
    /// </summary>
    public float Error => Scale.Delta(_log.Header.AxisScale, Expected, _log.Header.AxisScale, Actual, _axis);

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

    /// <summary>
    /// Returns the record value in IEC scale.
    /// </summary>
    /// <param name="recordType">The type of record (Expected or Actual).</param>
    /// <returns>The value in IEC scale.</returns>
    public float GetRecordInIec(RecordType recordType) =>
        Scale.ToIec(_log.Header.AxisScale, _axis, GetRecord(recordType));
}