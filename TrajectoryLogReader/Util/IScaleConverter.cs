using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

/// <summary>
/// Interface for converting axis values between different scales.
/// </summary>
public interface IScaleConverter
{
    /// <summary>
    /// Converts a value to IEC scale.
    /// </summary>
    float ToIec(Axis axis, float value);
    
    /// <summary>
    /// Converts a value from IEC scale.
    /// </summary>
    float FromIec(Axis axis, float value);
    
    /// <summary>
    /// Converts an MLC position to IEC scale.
    /// </summary>
    float MlcPositionToIec(int bank, float value);
    
    /// <summary>
    /// Converts an MLC position from IEC scale.
    /// </summary>
    float MlcPositionFromIec(int bank, float value);
}