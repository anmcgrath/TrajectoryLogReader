using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log.Axes
{
    public class MlcVelocityAccessor : IEnumerable<IAxisAccessor>
    {
        private readonly Dictionary<(int, int), IAxisAccessor> _velocityLookup;

        internal MlcVelocityAccessor(IEnumerable<MlcLeafAxisAccessor> leaves)
        {
            _velocityLookup = leaves.ToDictionary(
                l => (l.BankIndex, l.LeafIndex),
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

        public IAxisAccessor? GetLeaf(int bankIndex, int leafIndex)
        {
            if (_velocityLookup.TryGetValue((bankIndex, leafIndex), out var leaf))
                return leaf;
            return null;
        }

        public float RootMeanSquareError()
        {
            if (!_velocityLookup.Any()) return 0f;
            return Statistics.CalculateRootMeanSquareError(_velocityLookup.Values.SelectMany(l => l.DeltaValues));
        }

        public float MaxError()
        {
            if (!_velocityLookup.Any()) return 0f;
            return Statistics.CalculateMaxError(_velocityLookup.Values.SelectMany(l => l.DeltaValues));
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            var allDeltas = _velocityLookup.Values.SelectMany(l => l.DeltaValues).ToArray();
            return Histogram.FromData(allDeltas, nBins);
        }
    }
}