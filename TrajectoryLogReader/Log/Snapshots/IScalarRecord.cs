namespace TrajectoryLogReader.Log.Snapshots;

public interface IScalarRecord
{
    /// <summary>
    /// The expected position of the axis (in target scale if WithScale was called, otherwise native scale).
    /// </summary>
    public float Expected { get; }

    /// <summary>
    /// The actual position of the axis (in target scale if WithScale was called, otherwise native scale).
    /// </summary>
    public float Actual { get; }

    /// <summary>
    /// Actual - Expected (computed in effective scale with proper normalization for rotational axes)
    /// </summary>
    public float Error { get; }

    /// <summary>
    /// Returns the scalar record of type <paramref name="type"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public float GetRecord(RecordType type);

    /// <summary>
    /// Creates a new ScalarRecord with values converted to the specified scale.
    /// </summary>
    /// <param name="scale">The target scale for value conversion.</param>
    /// <returns>A new ScalarRecord configured to return values in the specified scale.</returns>
    public IScalarRecord WithScale(AxisScale scale);
}