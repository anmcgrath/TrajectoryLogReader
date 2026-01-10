using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Fluence;

/// <summary>
/// Field data for a beam snapshot. All axes values should be in IEC.
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
    float X1InMm { get; }

    /// <summary>
    /// Y1 position (in cm)
    /// </summary>
    float Y1InMm { get; }

    /// <summary>
    /// X2 position (in cm)
    /// </summary>
    float X2InMm { get; }

    /// <summary>
    /// Y2 position (in cm)
    /// </summary>
    float Y2InMm { get; }

    /// <summary>
    /// Gantry angle in degrees
    /// </summary>
    float GantryInDegrees { get; }

    /// <summary>
    /// The collimator angle in degrees
    /// </summary>
    float CollimatorInDegrees { get; }

    /// <summary>
    /// Return the leaf position (in mm) at bank <paramref name="bank"/> and <paramref name="leafIndex"/>
    /// </summary>
    /// <param name="bank"></param>
    /// <param name="leafIndex"></param>
    /// <returns></returns>
    float GetLeafPositionInMm(int bank, int leafIndex);

    /// <summary>
    /// The amount of MU delivered during this beam snapshot
    /// </summary>
    float DeltaMu { get; }
}