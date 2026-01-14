using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log.Axes
{
    public class MlcAxisAccessor : IEnumerable<MlcLeafAxisAccessor>
    {
        private readonly Dictionary<(int, int), MlcLeafAxisAccessor> _leafLookup;
        private readonly TrajectoryLog _log;

        internal MlcAxisAccessor(TrajectoryLog log, IEnumerable<MlcLeafAxisAccessor> leaves)
        {
            _log = log;
            _leafLookup = leaves.ToDictionary(l => (l.BankIndex, l.LeafIndex));
        }

        public IEnumerator<MlcLeafAxisAccessor> GetEnumerator()
        {
            return _leafLookup.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public MlcLeafAxisAccessor? GetLeaf(int bankIndex, int leafIndex)
        {
            if (_leafLookup.TryGetValue((bankIndex, leafIndex), out var leaf))
                return leaf;
            return null;
        }

        public MlcVelocityAccessor Velocity => new MlcVelocityAccessor(_leafLookup.Values);

        public float RootMeanSquareError()
        {
            if (!_leafLookup.Any()) return 0f;
            return Statistics.CalculateRootMeanSquareError(_leafLookup.Values.SelectMany(l => l.Deltas));
        }

        public float MaxError()
        {
            if (!_leafLookup.Any()) return 0f;
            return Statistics.CalculateMaxError(_leafLookup.Values.SelectMany(l => l.Deltas));
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            // Collect all deltas from all leaves
            var allDeltas = _leafLookup.Values.SelectMany(l => l.Deltas).ToArray();
            return Histogram.FromData(allDeltas, nBins);
        }
    }
}