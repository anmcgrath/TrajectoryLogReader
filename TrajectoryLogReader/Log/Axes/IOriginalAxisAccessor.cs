namespace TrajectoryLogReader.Log.Axes;

/// <summary>
/// The axis accessor accesses original (non-computed) axis data
/// </summary>
public interface IOriginalAxisAccessor
{
    public Axis Axis { get; }
}