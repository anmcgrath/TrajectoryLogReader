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

    internal FieldFluence(GridF grid, FluenceOptions options)
    {
        Grid = grid;
        Options = options;
    }
}