using System;
using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Log.Axes
{
    public class VelocityAxisAccessor : IAxisAccessor
    {
        private readonly IAxisAccessor _inner;
        private readonly double _samplingIntervalSeconds;

        public VelocityAxisAccessor(IAxisAccessor inner, double samplingIntervalMs)
        {
            _inner = inner;
            _samplingIntervalSeconds = samplingIntervalMs / 1000.0;
        }

        private IEnumerable<float> CalculateVelocity(IEnumerable<float> positions)
        {
            using var e = positions.GetEnumerator();
            if (!e.MoveNext()) yield break;

            float prev = e.Current;
            
            yield return 0f;

            while (e.MoveNext())
            {
                float current = e.Current;
                yield return (float)((current - prev) / _samplingIntervalSeconds);
                prev = current;
            }
        }

        public IEnumerable<float> Expected()
        {
            return CalculateVelocity(_inner.Expected());
        }

        public IEnumerable<float> Actual()
        {
            return CalculateVelocity(_inner.Actual());
        }

        public IEnumerable<float> Deltas()
        {
            // Velocity Delta = ActualVelocity - ExpectedVelocity
            using var exp = Expected().GetEnumerator();
            using var act = Actual().GetEnumerator();

            while (exp.MoveNext() && act.MoveNext())
            {
                yield return act.Current - exp.Current;
            }
        }

        public IAxisAccessor WithScale(AxisScale scale)
        {
            return new VelocityAxisAccessor(_inner.WithScale(scale), _samplingIntervalSeconds * 1000.0);
        }

        public float RootMeanSquareError()
        {
            return Statistics.CalculateRootMeanSquareError(Deltas());
        }

        public float MaxError()
        {
            return Statistics.CalculateMaxError(Deltas());
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            return Histogram.FromData(Deltas().ToArray(), nBins);
        }
    }
}