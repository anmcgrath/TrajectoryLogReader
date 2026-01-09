namespace TrajectoryLogReader.Fluence;

/// <summary>
/// A collection of field data snapshots. Implement this to build fluence
/// </summary>
public interface IFieldDataCollection : IEnumerable<IFieldData>
{
}