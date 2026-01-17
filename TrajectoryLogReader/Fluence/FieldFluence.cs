namespace TrajectoryLogReader.Fluence;

/// <summary>
/// Represents a calculated fluence map for a field.
/// </summary>
public class FieldFluence
{
    /// <summary>
    /// The underlying floating-point grid containing fluence values (MU).
    /// </summary>
    public GridF Grid { get; }

    /// <summary>
    /// The options used to generate the fluence
    /// </summary>
    public FluenceOptions Options { get; }

    /// <summary>
    /// The rotated jaw corners for each unique jaw/collimator configuration in the field.
    /// Each array of 4 points represents one jaw rectangle rotated by collimator angle.
    /// Points are ordered: TopRight, TopLeft, BottomLeft, BottomRight.
    /// </summary>
    public List<Point[]> JawOutlines { get; }

    internal FieldFluence(GridF grid, FluenceOptions options, List<Point[]> jawOutlines)
    {
        Grid = grid;
        Options = options;
        JawOutlines = jawOutlines;
    }
}