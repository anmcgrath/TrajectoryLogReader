using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Log.Axes
{
    public interface IAxisAccessor
    {
        IEnumerable<float> Expected();
        IEnumerable<float> Actual();
        IEnumerable<float> Deltas();
        IAxisAccessor WithScale(AxisScale scale);
    }
}