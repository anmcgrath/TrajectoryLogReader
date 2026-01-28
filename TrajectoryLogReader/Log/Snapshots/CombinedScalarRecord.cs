using System;

namespace TrajectoryLogReader.Log.Snapshots
{
    public class CombinedScalarRecord : IScalarRecord
    {
        private readonly IScalarRecord _record1;
        private readonly IScalarRecord _record2;
        private readonly Func<float, float, float> _combiner;

        public CombinedScalarRecord(IScalarRecord record1, IScalarRecord record2, Func<float, float, float> combiner)
        {
            _record1 = record1;
            _record2 = record2;
            _combiner = combiner;
        }

        public IScalarRecord WithScale(AxisScale scale)
        {
            return new CombinedScalarRecord(_record1.WithScale(scale), _record2.WithScale(scale), _combiner);
        }

        public float Expected => _combiner(_record1.Expected, _record2.Expected);

        public float Actual => _combiner(_record1.Actual, _record2.Actual);

        public float GetRecord(RecordType type)
        {
            return _combiner(_record1.GetRecord(type), _record2.GetRecord(type));
        }

        public float Error => _combiner(_record1.Error, _record2.Error);

        public IScalarRecord GetDelta(TimeSpan? timeSpan = null)
        {
            return new CombinedScalarRecord(_record1.GetDelta(timeSpan), _record2.GetDelta(timeSpan), _combiner);
        }
    }
}