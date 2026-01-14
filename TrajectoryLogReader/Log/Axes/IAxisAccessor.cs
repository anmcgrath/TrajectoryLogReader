using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Log.Axes
{
    public interface IAxisAccessor
    {
        IEnumerable<float> Expected { get; }
        IEnumerable<float> Actual { get; }
        IEnumerable<float> Deltas { get; }
        IAxisAccessor WithScale(AxisScale scale);

        float RootMeanSquareError();
        float MaxError();
        Histogram ErrorHistogram(int nBins = 20);
    }
}