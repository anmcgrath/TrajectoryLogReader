using System;
using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Log.Axes
{
    public abstract class AxisAccessorBase : IAxisAccessorInternal
    {
        public abstract int TimeInMs { get; }
        public abstract int SampleRateInMs { get; }

        public abstract IEnumerable<float> ExpectedValues { get; }

        public abstract IEnumerable<float> ActualValues { get; }

        public abstract IEnumerable<float> ErrorValues { get; }

        public abstract IAxisAccessor WithScale(AxisScale scale);

        public float RootMeanSquareError()
        {
            return Statistics.CalculateRootMeanSquareError(ErrorValues);
        }

        public float MaxError()
        {
            return Statistics.CalculateMaxError(ErrorValues);
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            return Histogram.FromData(ErrorValues.ToArray(), nBins);
        }

        public IAxisAccessor WithFilter(IAxisAccessor filterAxis, RecordType recordType, Func<float, bool> predicate)
        {
            return new FilteredAxisAccessor(this, filterAxis, recordType, predicate);
        }

        public abstract AxisScale GetSourceScale();
        public abstract AxisScale GetEffectiveScale();

        public IAxisAccessor GetDelta(TimeSpan? timeSpan = null)
        {
            return new DeltaAxisAccessor(this, SampleRateInMs, timeSpan);
        }
    }
}
