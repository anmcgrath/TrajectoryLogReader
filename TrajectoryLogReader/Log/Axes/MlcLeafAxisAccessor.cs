using TrajectoryLogReader.LogStatistics;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Axes
{
    public class MlcLeafAxisAccessor : IAxisAccessor
    {
        private readonly TrajectoryLog _log;
        private readonly int _bankIndex;
        private readonly int _leafIndex;
        private readonly int _startIndex;
        private readonly int _endIndex;
        private readonly AxisScale _targetScale;
        public int TimeInMs => (_endIndex - _startIndex) * _log.Header.SamplingIntervalInMS;

        public int BankIndex => _bankIndex;
        public int LeafIndex => _leafIndex;

        internal MlcLeafAxisAccessor(TrajectoryLog log, int bankIndex, int leafIndex, int startIndex, int endIndex,
            AxisScale? targetScale = null)
        {
            _log = log;
            _bankIndex = bankIndex;
            _leafIndex = leafIndex;
            _startIndex = startIndex;
            _endIndex = endIndex;
            _targetScale = targetScale ?? _log.Header.AxisScale;
        }

        private float[]? _expected;

        public IEnumerable<float> Expected
        {
            get
            {
                if (_expected == null)
                {
                    _expected = GetExpected().ToArray();
                }

                return _expected;
            }
        }

        private IEnumerable<float> GetExpected()
        {
            for (int i = _startIndex; i <= _endIndex; i++)
            {
                var val = _log.GetMlcPosition(i, RecordType.ExpectedPosition, _leafIndex, _bankIndex);
                yield return Scale.ConvertMlc(_log.Header.AxisScale, _targetScale, _bankIndex, val);
            }
        }

        private float[]? _actual;

        public IEnumerable<float> Actual
        {
            get
            {
                if (_actual == null)
                {
                    _actual = GetActual().ToArray();
                }

                return _actual;
            }
        }

        private IEnumerable<float> GetActual()
        {
            for (int i = _startIndex; i <= _endIndex; i++)
            {
                var val = _log.GetMlcPosition(i, RecordType.ActualPosition, _leafIndex, _bankIndex);
                yield return Scale.ConvertMlc(_log.Header.AxisScale, _targetScale, _bankIndex, val);
            }
        }

        private float[]? _deltas;

        public IEnumerable<float> Deltas
        {
            get
            {
                if (_deltas == null)
                {
                    _deltas = GetDeltas().ToArray();
                }

                return _deltas;
            }
        }

        private IEnumerable<float> GetDeltas()
        {
            for (int i = _startIndex; i <= _endIndex; i++)
            {
                var exp = _log.GetMlcPosition(i, RecordType.ExpectedPosition, _leafIndex, _bankIndex);
                var act = _log.GetMlcPosition(i, RecordType.ActualPosition, _leafIndex, _bankIndex);

                var expConv = Scale.ConvertMlc(_log.Header.AxisScale, _targetScale, _bankIndex, exp);
                var actConv = Scale.ConvertMlc(_log.Header.AxisScale, _targetScale, _bankIndex, act);

                yield return actConv - expConv;
            }
        }

        public IAxisAccessor WithScale(AxisScale scale)
        {
            return new MlcLeafAxisAccessor(_log, _bankIndex, _leafIndex, _startIndex, _endIndex, scale);
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

        public IAxisAccessor GetVelocity()
        {
            return new VelocityAxisAccessor(this, _log.Header.SamplingIntervalInMS);
        }
    }
}