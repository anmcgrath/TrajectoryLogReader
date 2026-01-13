using System.Collections;

namespace TrajectoryLogReader.Log;

/// <summary>
/// Represents a collection of measurement data snapshots from a trajectory log.
/// </summary>
public class SnapshotCollection : IEnumerable<Snapshot>
{
    internal TrajectoryLog Log { get; }
    private readonly int _startIndex;
    private readonly int _endIndex;

    /// <summary>
    /// The number of snapshots in the collection.
    /// </summary>
    public int Count => _endIndex < 0 ? 0 : (_endIndex - _startIndex + 1);

    internal SnapshotCollection(TrajectoryLog log, int startIndex, int endIndex)
    {
        Log = log;
        _startIndex = startIndex;
        _endIndex = endIndex;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<Snapshot> GetEnumerator()
    {
        return new MeasurementDataEnumerator(Log, _startIndex, _endIndex);
    }

    /// <summary>
    /// Returns the last measurement data snapshot in the collection.
    /// </summary>
    /// <returns>The last snapshot.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
    public Snapshot Last()
    {
        if (Count == 0)
            throw new InvalidOperationException();
        return new Snapshot(_endIndex, Log);
    }

    /// <summary>
    /// Returns the first measurement data snapshot in the collection.
    /// </summary>
    /// <returns>The first snapshot.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
    public Snapshot First()
    {
        if (Count == 0)
            throw new InvalidOperationException();
        return new Snapshot(_startIndex, Log);
    }

    /// <summary>
    /// Returns the last measurement data snapshot, or null if the collection is empty.
    /// </summary>
    /// <returns>The last snapshot or null.</returns>
    public Snapshot LastOrDefault()
    {
        return Count == 0 ? null : Last();
    }

    /// <summary>
    /// Returns the first measurement data snapshot, or null if the collection is empty.
    /// </summary>
    /// <returns>The first snapshot or null.</returns>
    public Snapshot FirstOrDefault()
    {
        return Count == 0 ? null : First();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}