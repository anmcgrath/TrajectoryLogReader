using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Log.Axes
{
    public interface IAxisAccessor
    {
        IEnumerable<float> Expected();
        IEnumerable<float> Actual();
        IEnumerable<float> Deltas();
        IAxisAccessor WithScale(AxisScale scale);

        float RootMeanSquareError();
        float MaxError();
        Histogram ErrorHistogram(int nBins = 20);
    }
}