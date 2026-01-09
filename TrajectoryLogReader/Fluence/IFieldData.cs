using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Fluence;

/// <summary>
/// Field data for a beam snapshot. All axes values should be in Varian IEC.
/// </summary>
public interface IFieldData
{
    /// <summary>
    /// MLC model
    /// </summary>
    IMLCModel Mlc { get; }

    /// <summary>
    /// X1 position (in cm)
    /// </summary>
    float X1InCm { get; }

    /// <summary>
    /// Y1 position (in cm)
    /// </summary>
    float Y1InCm { get; }

    /// <summary>
    /// X2 position (in cm)
    /// </summary>
    float X2InCm { get; }

    /// <summary>
    /// Y2 position (in cm)
    /// </summary>
    float Y2InCm { get; }

    /// <summary>
    /// Gantry angle in degrees
    /// </summary>
    float GantryInDegrees { get; }

    /// <summary>
    /// The collimator angle in degrees
    /// </summary>
    float CollimatorInDegrees { get; }

    /// <summary>
    /// The MLC positions accessible through [bankIndex, leafIndex]
    /// </summary>
    float[,] MlcPositionsInCm { get; }

    /// <summary>
    /// The amount of MU delivered during this beam snapshot
    /// </summary>
    float DeltaMu { get; }
}