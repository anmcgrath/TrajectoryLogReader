using System;
using System.Collections.Generic;
using System.Linq;

namespace TrajectoryLogReader.Log.Axes
{
    public class VelocityAxisAccessor : AxisAccessorBase
    {
        private readonly IAxisAccessor _inner;
        private readonly double _samplingIntervalSeconds;
        public override int TimeInMs => _inner.TimeInMs;

        private float[]? _expected;
        private float[]? _actual;
        private float[]? _errors;

        public VelocityAxisAccessor(IAxisAccessor inner, double samplingIntervalMs)
        {
            _inner = inner;
            _samplingIntervalSeconds = samplingIntervalMs / 1000.0;
        }

        private IEnumerable<float> CalculateVelocity(IEnumerable<float> positions)
        {
            using var e = positions.GetEnumerator();
            if (!e.MoveNext()) yield break;

            float prev = e.Current;

            yield return 0f;

            while (e.MoveNext())
            {
                float current = e.Current;
                yield return (float)((current - prev) / _samplingIntervalSeconds);
                prev = current;
            }
        }

        public override IEnumerable<float> ExpectedValues
        {
            get
            {
                if (_expected == null)
                {
                    _expected = CalculateVelocity(_inner.ExpectedValues).ToArray();
                }

                return _expected;
            }
        }

        public override IEnumerable<float> ActualValues
        {
            get
            {
                if (_actual == null)
                {
                    _actual = CalculateVelocity(_inner.ActualValues).ToArray();
                }

                return _actual;
            }
        }

        public override IEnumerable<float> ErrorValues
        {
            get
            {
                if (_errors == null)
                {
                    _errors = GetErrors().ToArray();
                }

                return _errors;
            }
        }

        private IEnumerable<float> GetErrors()
        {
            // Velocity Delta = ActualVelocity - ExpectedVelocity
            using var exp = ExpectedValues.GetEnumerator();
            using var act = ActualValues.GetEnumerator();

            while (exp.MoveNext() && act.MoveNext())
            {
                yield return act.Current - exp.Current;
            }
        }

        public override IAxisAccessor WithScale(AxisScale scale)
        {
            return new VelocityAxisAccessor(_inner.WithScale(scale), _samplingIntervalSeconds * 1000.0);
        }
    }
}