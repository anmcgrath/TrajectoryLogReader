using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Log.Axes
{
    public interface IAxisAccessor
    {
        /// <summary>
        /// The expected values of this axis
        /// </summary>
        IEnumerable<float> ExpectedValues { get; }

        /// <summary>
        /// The actual values recorded for this axis
        /// </summary>
        IEnumerable<float> ActualValues { get; }

        /// <summary>
        /// The difference (Actual - Expected) for this axis.
        /// </summary>
        IEnumerable<float> ErrorValues { get; }

        /// <summary>
        /// Create a new axis accessor with results in the scale specified
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        IAxisAccessor WithScale(AxisScale scale);

        /// <summary>
        /// Calculate the root mean square error for this axis.
        /// </summary>
        /// <returns></returns>
        float RootMeanSquareError();

        /// <summary>
        /// Calculate the maximum error (by magnitude) for this axis
        /// </summary>
        /// <returns></returns>
        float MaxError();

        /// <summary>
        /// Compute an error histogram this this axis
        /// </summary>
        /// <param name="nBins"></param>
        /// <returns></returns>
        Histogram ErrorHistogram(int nBins = 20);

        /// <summary>
        /// The time (in ms) for this data recording.
        /// </summary>
        public int TimeInMs { get; }
    }
}