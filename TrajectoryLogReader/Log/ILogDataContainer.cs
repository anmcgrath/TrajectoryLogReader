using TrajectoryLogReader.Log.Axes;
using TrajectoryLogReader.Log.Snapshots;

namespace TrajectoryLogReader.Log;

public interface ILogDataContainer
{
    public SnapshotCollection Snapshots { get; }
    public LogAxes Axes { get; }
}