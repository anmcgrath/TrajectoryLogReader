using System;
using System.Collections.Generic;
using System.Linq;

namespace TrajectoryLogReader.Log.Axes
{
    public class CombinedAxisAccessor : AxisAccessorBase
    {
        private readonly IAxisAccessor _axis1;
        private readonly IAxisAccessor _axis2;
        private readonly Func<float, float, float> _combiner;
        public override int TimeInMs => _axis1.TimeInMs;
        public override int SampleRateInMs => _axis1.SampleRateInMs;
        
        public override AxisScale GetSourceScale() => _axis1.GetSourceScale();

        public override AxisScale GetEffectiveScale() => _axis1.GetEffectiveScale();

        private float[]? _expected;
        private float[]? _actual;
        private float[]? _errors;

        public CombinedAxisAccessor(IAxisAccessor axis1, IAxisAccessor axis2, Func<float, float, float> combiner)
        {
            _axis1 = axis1;
            _axis2 = axis2;
            _combiner = combiner;
        }

        public override IEnumerable<float> ExpectedValues
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
            using var e1 = _axis1.ExpectedValues.GetEnumerator();
            using var e2 = _axis2.ExpectedValues.GetEnumerator();

            while (e1.MoveNext() && e2.MoveNext())
            {
                yield return _combiner(e1.Current, e2.Current);
            }
        }

        public override IEnumerable<float> ActualValues
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
            using var e1 = _axis1.ActualValues.GetEnumerator();
            using var e2 = _axis2.ActualValues.GetEnumerator();

            while (e1.MoveNext() && e2.MoveNext())
            {
                yield return _combiner(e1.Current, e2.Current);
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
            using var e1 = _axis1.ErrorValues.GetEnumerator();
            using var e2 = _axis2.ErrorValues.GetEnumerator();

            while (e1.MoveNext() && e2.MoveNext())
            {
                yield return _combiner(e1.Current, e2.Current);
            }
        }

        public override IAxisAccessor WithScale(AxisScale scale)
        {
            return new CombinedAxisAccessor(_axis1.WithScale(scale), _axis2.WithScale(scale), _combiner);
        }
    }
}