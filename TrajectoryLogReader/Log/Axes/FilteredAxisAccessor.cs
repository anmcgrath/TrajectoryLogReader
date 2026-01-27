using System;
using System.Collections.Generic;
using System.Linq;

namespace TrajectoryLogReader.Log.Axes
{
    /// <summary>
    /// An axis accessor that filters values based on another axis.
    /// For example, to only include values where DeltaMu > 0.
    /// </summary>
    public class FilteredAxisAccessor : AxisAccessorBase
    {
        private readonly IAxisAccessor _source;
        private readonly IAxisAccessor _filterAxis;
        private readonly RecordType _recordType;
        private readonly Func<float, bool> _predicate;

        public override int TimeInMs => _source.TimeInMs;
        public override int SampleRateInMs => _source.SampleRateInMs;
        

        private float[]? _expected;
        private float[]? _actual;
        private float[]? _errors;

        public FilteredAxisAccessor(IAxisAccessor source, IAxisAccessor filterAxis, RecordType recordType,
            Func<float, bool> predicate)
        {
            _source = source;
            _filterAxis = filterAxis;
            _recordType = recordType;
            _predicate = predicate;
        }

        public override IEnumerable<float> ExpectedValues
        {
            get
            {
                if (_expected == null)
                {
                    _expected = GetFiltered(_source.ExpectedValues).ToArray();
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
                    _actual = GetFiltered(_source.ActualValues).ToArray();
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
                    _errors = GetFiltered(_source.ErrorValues).ToArray();
                }

                return _errors;
            }
        }

        private IEnumerable<float> GetFiltered(IEnumerable<float> values)
        {
            using var sourceEnum = values.GetEnumerator();
            using var filterEnum = _recordType == RecordType.ExpectedPosition
                ? _filterAxis.ExpectedValues.GetEnumerator()
                : _filterAxis.ActualValues.GetEnumerator();

            while (sourceEnum.MoveNext() && filterEnum.MoveNext())
            {
                if (_predicate(filterEnum.Current))
                {
                    yield return sourceEnum.Current;
                }
            }
        }

        public override IAxisAccessor WithScale(AxisScale scale)
        {
            return new FilteredAxisAccessor(_source.WithScale(scale), _filterAxis.WithScale(scale), _recordType,
                _predicate);
        }
        
        public override AxisScale GetSourceScale() => _source.GetSourceScale();

        public override AxisScale GetEffectiveScale() => _source.GetEffectiveScale();
    }
}