using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log.Axes
{
    public class MlcAxisAccessor
    {
        private readonly Dictionary<(int, int), MlcLeafAxisAccessor> _leafLookup;
        private readonly TrajectoryLog _log;

        public IEnumerable<MlcLeafAxisAccessor> Leaves => _leafLookup.Values;

        internal MlcAxisAccessor(TrajectoryLog log, IEnumerable<MlcLeafAxisAccessor> leaves)
        {
            _log = log;
            _leafLookup = leaves.ToDictionary(l => (l.BankIndex, l.LeafIndex));
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
            return Statistics.CalculateRootMeanSquareError(_leafLookup.Values.SelectMany(l => l.ErrorValues));
        }

        public float MaxError()
        {
            if (!_leafLookup.Any()) return 0f;
            return Statistics.CalculateMaxError(_leafLookup.Values.SelectMany(l => l.ErrorValues));
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            // Collect all deltas from all leaves
            var allDeltas = _leafLookup.Values.SelectMany(l => l.ErrorValues).ToArray();
            return Histogram.FromData(allDeltas, nBins);
        }
    }
}