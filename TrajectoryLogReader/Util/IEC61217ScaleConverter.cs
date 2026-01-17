using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

/// <summary>
/// Identity converter for true IEC 61217 coordinate system.
/// Since IEC is used as the intermediate representation for all conversions,
/// this converter simply passes values through unchanged.
/// </summary>
public class IEC61217ScaleConverter : IScaleConverter
{
    /// <inheritdoc />
    public float ToIec(Axis axis, float value) => value;

    /// <inheritdoc />
    public float FromIec(Axis axis, float value) => value;

    /// <inheritdoc />
    public float MlcPositionToIec(int bank, float value) => value;

    /// <inheritdoc />
    public float MlcPositionFromIec(int bank, float value) => value;
}
