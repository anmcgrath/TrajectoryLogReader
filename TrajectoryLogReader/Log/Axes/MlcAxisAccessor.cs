using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Axes
{
    public class MlcAxisAccessor : IEnumerable<MlcLeafAxisAccessor>
    {
        private readonly List<MlcLeafAxisAccessor> _leaves;
        private readonly TrajectoryLog _log;

        internal MlcAxisAccessor(TrajectoryLog log, IEnumerable<MlcLeafAxisAccessor> leaves)
        {
            _log = log;
            _leaves = leaves.ToList();
        }

        public IEnumerator<MlcLeafAxisAccessor> GetEnumerator()
        {
            return _leaves.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public MlcLeafAxisAccessor? GetLeaf(int bank, int leafIndex)
        {
            return _leaves.FirstOrDefault(l => l.Bank == bank && l.LeafIndex == leafIndex);
        }

        public float RootMeanSquareError()
        {
            if (!_leaves.Any()) return 0f;
            return Statistics.CalculateRootMeanSquareError(_leaves.SelectMany(l => l.Deltas()));
        }

        public float MaxError()
        {
            if (!_leaves.Any()) return 0f;
            return Statistics.CalculateMaxError(_leaves.SelectMany(l => l.Deltas()));
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            // Collect all deltas from all leaves
            var allDeltas = _leaves.SelectMany(l => l.Deltas()).ToArray();
            return Histogram.FromData(allDeltas, nBins);
        }
    }
}