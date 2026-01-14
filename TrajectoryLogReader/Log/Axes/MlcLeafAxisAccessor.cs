using System;
using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;
using TrajectoryLogReader.Util;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log.Axes
{
    public class MlcLeafAxisAccessor : IAxisAccessor
    {
        private readonly TrajectoryLog _log;
        private readonly Bank _bank;
        private readonly int _leafIndex;
        private readonly int _startIndex;
        private readonly int _endIndex;
        private readonly AxisScale _targetScale;

        public Bank Bank => _bank;
        public int LeafIndex => _leafIndex;

        internal MlcLeafAxisAccessor(TrajectoryLog log, Bank bank, int leafIndex, int startIndex, int endIndex, AxisScale? targetScale = null)
        {
            _log = log;
            _bank = bank;
            _leafIndex = leafIndex;
            _startIndex = startIndex;
            _endIndex = endIndex;
            _targetScale = targetScale ?? _log.Header.AxisScale;
        }

        public IEnumerable<float> Expected()
        {
            for (int i = _startIndex; i <= _endIndex; i++)
            {
                var val = _log.GetMlcPosition(i, RecordType.ExpectedPosition, _leafIndex, (int)_bank);
                yield return Scale.ConvertMlc(_log.Header.AxisScale, _targetScale, (int)_bank, val);
            }
        }

        public IEnumerable<float> Actual()
        {
            for (int i = _startIndex; i <= _endIndex; i++)
            {
                var val = _log.GetMlcPosition(i, RecordType.ActualPosition, _leafIndex, (int)_bank);
                yield return Scale.ConvertMlc(_log.Header.AxisScale, _targetScale, (int)_bank, val);
            }
        }

        public IEnumerable<float> Deltas()
        {
            for (int i = _startIndex; i <= _endIndex; i++)
            {
                var exp = _log.GetMlcPosition(i, RecordType.ExpectedPosition, _leafIndex, (int)_bank);
                var act = _log.GetMlcPosition(i, RecordType.ActualPosition, _leafIndex, (int)_bank);

                var expConv = Scale.ConvertMlc(_log.Header.AxisScale, _targetScale, (int)_bank, exp);
                var actConv = Scale.ConvertMlc(_log.Header.AxisScale, _targetScale, (int)_bank, act);

                yield return actConv - expConv;
            }
        }

        public IAxisAccessor WithScale(AxisScale scale)
        {
            return new MlcLeafAxisAccessor(_log, _bank, _leafIndex, _startIndex, _endIndex, scale);
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