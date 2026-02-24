namespace TrajectoryLogReader.Fluence;

/// <summary>
/// A time-ordered collection of delivery snapshots suitable for fluence reconstruction.
/// Implementations are expected to yield snapshots in IEC 61217 coordinates with
/// millimeter units for linear axes and incremental MU in <see cref="IFieldData.DeltaMu"/>.
/// </summary>
public interface IFieldDataCollection : IEnumerable<IFieldData>
{
}