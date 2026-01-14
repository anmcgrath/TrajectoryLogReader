using System.Collections.Generic;
using System.Linq;
using TrajectoryLogReader.LogStatistics;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Log.Axes
{
    internal class AxisAccessor : IAxisAccessor
    {
        private readonly TrajectoryLog _log;
        private readonly Axis _axis;
        private readonly int _startIndex;
        private readonly int _endIndex;
        private readonly AxisScale _targetScale;

        /// <summary>
        /// The time (in ms) for this data
        /// </summary>
        public int TimeInMs => (_endIndex - _startIndex) * _log.Header.SamplingIntervalInMS;

        public AxisAccessor(TrajectoryLog log, Axis axis, int startIndex, int endIndex, AxisScale? targetScale = null)
        {
            _log = log;
            _axis = axis;
            _startIndex = startIndex;
            _endIndex = endIndex;
            _targetScale = targetScale ?? _log.Header.AxisScale;
        }

        private float[]? _expected;

        public IEnumerable<float> ExpectedValues
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
                var val = _log.GetAxisData(_axis, i, RecordType.ExpectedPosition);
                yield return Scale.Convert(_log.Header.AxisScale, _targetScale, _axis, val);
            }
        }

        private float[]? _actual;

        public IEnumerable<float> ActualValues
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
                var val = _log.GetAxisData(_axis, i, RecordType.ActualPosition);
                yield return Scale.Convert(_log.Header.AxisScale, _targetScale, _axis, val);
            }
        }

        private float[]? _deltas;

        public IEnumerable<float> DeltaValues
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
                var exp = _log.GetAxisData(_axis, i, RecordType.ExpectedPosition);
                var act = _log.GetAxisData(_axis, i, RecordType.ActualPosition);

                var expConv = Scale.Convert(_log.Header.AxisScale, _targetScale, _axis, exp);
                var actConv = Scale.Convert(_log.Header.AxisScale, _targetScale, _axis, act);

                var diff = actConv - expConv;

                if (IsRotational(_axis))
                {
                    diff = Normalize(diff, 360);
                }
                else if (IsCouch(_axis))
                {
                    // Check for 1000 wrap (mm or IEC offset)
                    diff = Normalize(diff, 1000);
                    // Check for 100 wrap (cm or Native offset)
                    diff = Normalize(diff, 100);
                }

                yield return diff;
            }
        }

        public IAxisAccessor WithScale(AxisScale scale)
        {
            return new AxisAccessor(_log, _axis, _startIndex, _endIndex, scale);
        }

        public float RootMeanSquareError()
        {
            return Statistics.CalculateRootMeanSquareError(DeltaValues);
        }

        public float MaxError()
        {
            return Statistics.CalculateMaxError(DeltaValues);
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            return Histogram.FromData(DeltaValues.ToArray(), nBins);
        }

        private float Normalize(float value, float period)
        {
            if (value > period / 2) return value - period;
            if (value <= -period / 2) return value + period;
            return value;
        }

        private bool IsRotational(Axis axis)
        {
            return axis == Axis.GantryRtn ||
                   axis == Axis.CollRtn ||
                   axis == Axis.CouchRtn ||
                   axis == Axis.CouchPitch ||
                   axis == Axis.CouchRoll;
        }

        private bool IsCouch(Axis axis)
        {
            return axis == Axis.CouchVrt ||
                   axis == Axis.CouchLng ||
                   axis == Axis.CouchLat;
        }

        public VelocityAxisAccessor Velocity => new VelocityAxisAccessor(this, _log.Header.SamplingIntervalInMS);
    }
}