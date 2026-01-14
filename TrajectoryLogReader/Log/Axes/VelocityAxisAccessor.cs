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
        public int TimeInMs => _inner.TimeInMs;

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

        public IEnumerable<float> ExpectedValues => CalculateVelocity(_inner.ExpectedValues);

        public IEnumerable<float> ActualValues => CalculateVelocity(_inner.ActualValues);

        public IEnumerable<float> ErrorValues
        {
            get
            {
                // Velocity Delta = ActualVelocity - ExpectedVelocity
                using var exp = ExpectedValues.GetEnumerator();
                using var act = ActualValues.GetEnumerator();

                while (exp.MoveNext() && act.MoveNext())
                {
                    yield return act.Current - exp.Current;
                }
            }
        }

        public IAxisAccessor WithScale(AxisScale scale)
        {
            return new VelocityAxisAccessor(_inner.WithScale(scale), _samplingIntervalSeconds * 1000.0);
        }

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
    }
}