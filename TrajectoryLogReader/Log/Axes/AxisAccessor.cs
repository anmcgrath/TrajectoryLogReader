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

        public AxisAccessor(TrajectoryLog log, Axis axis, int startIndex, int endIndex, AxisScale? targetScale = null)
        {
            _log = log;
            _axis = axis;
            _startIndex = startIndex;
            _endIndex = endIndex;
            _targetScale = targetScale ?? _log.Header.AxisScale;
        }

        public IEnumerable<float> Expected()
        {
            for (int i = _startIndex; i <= _endIndex; i++)
            {
                var val = _log.GetAxisData(_axis, i, RecordType.ExpectedPosition);
                yield return Scale.Convert(_log.Header.AxisScale, _targetScale, _axis, val);
            }
        }

        public IEnumerable<float> Actual()
        {
            for (int i = _startIndex; i <= _endIndex; i++)
            {
                var val = _log.GetAxisData(_axis, i, RecordType.ActualPosition);
                yield return Scale.Convert(_log.Header.AxisScale, _targetScale, _axis, val);
            }
        }

        public IEnumerable<float> Deltas()
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
            double sumSq = 0;
            int count = 0;
            foreach (var diff in Deltas())
            {
                sumSq += diff * diff;
                count++;
            }
            return count == 0 ? 0 : (float)Math.Sqrt(sumSq / count);
        }

        public float MaxError()
        {
            float maxError = 0f;
            float maxErrorAbs = 0f;

            foreach (var diff in Deltas())
            {
                if (Math.Abs(diff) > maxErrorAbs)
                {
                    maxError = diff;
                    maxErrorAbs = Math.Abs(diff);
                }
            }
            return maxError;
        }

        public Histogram ErrorHistogram(int nBins = 20)
        {
            return Histogram.FromData(Deltas().ToArray(), nBins);
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
    }
}