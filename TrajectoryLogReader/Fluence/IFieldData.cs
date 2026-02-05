using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Fluence;

/// <summary>
/// Field geometry and dose-weighting information for a single delivery snapshot, expressed
/// in IEC 61217 conventions. Fluence reconstruction assumes these values are already in a
/// consistent coordinate system and in millimeters for linear axes.
/// </summary>
public interface IFieldData
{
    /// <summary>
    /// The MLC model used to interpret leaf indexing, widths, and bank conventions.
    /// </summary>
    IMLCModel Mlc { get; }

    /// <summary>
    /// X1 jaw position in millimeters at isocenter.
    /// </summary>
    float X1InMm { get; }

    /// <summary>
    /// Y1 jaw position in millimeters at isocenter.
    /// </summary>
    float Y1InMm { get; }

    /// <summary>
    /// X2 jaw position in millimeters at isocenter.
    /// </summary>
    float X2InMm { get; }

    /// <summary>
    /// Y2 jaw position in millimeters at isocenter.
    /// </summary>
    float Y2InMm { get; }

    /// <summary>
    /// Gantry angle in degrees using IEC 61217 rotation conventions.
    /// </summary>
    float GantryInDegrees { get; }

    /// <summary>
    /// Collimator angle in degrees using IEC 61217 rotation conventions.
    /// </summary>
    float CollimatorInDegrees { get; }

    /// <summary>
    /// Returns the leaf tip position in millimeters for a given bank and leaf index.
    /// Bank indexing is model-dependent but in this library bank 0 typically represents
    /// the A/X2 side and bank 1 the B/X1 side.
    /// </summary>
    /// <param name="bank">The leaf bank index (for example 0 or 1).</param>
    /// <param name="leafIndex">The zero-based leaf pair index within the bank.</param>
    /// <returns>The leaf tip position in millimeters.</returns>
    float GetLeafPositionInMm(int bank, int leafIndex);

    /// <summary>
    /// The incremental MU delivered during this snapshot. This should be non-negative and
    /// is used as the dose-weight for fluence accumulation.
    /// </summary>
    float DeltaMu { get; }

    bool IsBeamHold();
}