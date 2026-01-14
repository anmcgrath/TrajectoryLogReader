using System;
using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Log.Axes
{
    public class CombinedAxisAccessor : IAxisAccessor
    {
        private readonly IAxisAccessor _axis1;
        private readonly IAxisAccessor _axis2;
        private readonly Func<float, float, float> _combiner;

        public CombinedAxisAccessor(IAxisAccessor axis1, IAxisAccessor axis2, Func<float, float, float> combiner)
        {
            _axis1 = axis1;
            _axis2 = axis2;
            _combiner = combiner;
        }

        public IEnumerable<float> Expected
        {
            get
            {
                using var e1 = _axis1.Expected.GetEnumerator();
                using var e2 = _axis2.Expected.GetEnumerator();

                while (e1.MoveNext() && e2.MoveNext())
                {
                    yield return _combiner(e1.Current, e2.Current);
                }
            }
        }

        public IEnumerable<float> Actual
        {
            get
            {
                using var e1 = _axis1.Actual.GetEnumerator();
                using var e2 = _axis2.Actual.GetEnumerator();

                while (e1.MoveNext() && e2.MoveNext())
                {
                    yield return _combiner(e1.Current, e2.Current);
                }
            }
        }

        public IEnumerable<float> Deltas
        {
            get
            {
                using var e1 = _axis1.Deltas.GetEnumerator();
                using var e2 = _axis2.Deltas.GetEnumerator();

                while (e1.MoveNext() && e2.MoveNext())
                {
                    yield return _combiner(e1.Current, e2.Current);
                }
            }
        }

        public IAxisAccessor WithScale(AxisScale scale)
        {
            return new CombinedAxisAccessor(_axis1.WithScale(scale), _axis2.WithScale(scale), _combiner);
        }

        public float RootMeanSquareError()
        {
            return Statistics.CalculateRootMeanSquareError(Deltas);
        }

        public float MaxError()
        {
            return Statistics.CalculateMaxError(Deltas);
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            return Histogram.FromData(Deltas.ToArray(), nBins);
        }
    }
}