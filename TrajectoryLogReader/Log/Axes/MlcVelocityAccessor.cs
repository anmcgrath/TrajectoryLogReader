using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log.Axes
{
    public class MlcVelocityAccessor : IEnumerable<IAxisAccessor>
    {
        private readonly Dictionary<(Bank, int), IAxisAccessor> _velocityLookup;

        internal MlcVelocityAccessor(IEnumerable<MlcLeafAxisAccessor> leaves)
        {
            _velocityLookup = leaves.ToDictionary(
                l => (l.Bank, l.LeafIndex), 
                l => l.GetVelocity());
        }

        public IEnumerator<IAxisAccessor> GetEnumerator()
        {
            return _velocityLookup.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IAxisAccessor? this[Bank bank, int leafIndex]
        {
            get
            {
                _velocityLookup.TryGetValue((bank, leafIndex), out var v);
                return v;
            }
        }

        public float RootMeanSquareError()
        {
            if (!_velocityLookup.Any()) return 0f;
            return Statistics.CalculateRootMeanSquareError(_velocityLookup.Values.SelectMany(l => l.Deltas()));
        }

        public float MaxError()
        {
            if (!_velocityLookup.Any()) return 0f;
            return Statistics.CalculateMaxError(_velocityLookup.Values.SelectMany(l => l.Deltas()));
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            var allDeltas = _velocityLookup.Values.SelectMany(l => l.Deltas()).ToArray();
            return Histogram.FromData(allDeltas, nBins);
        }
    }
}