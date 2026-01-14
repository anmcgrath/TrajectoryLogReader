using System;
using System.Collections.Generic;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log.Axes
{
    public class LogAxes
    {
        private readonly TrajectoryLog _log;
        private readonly int _startIndex;
        private readonly int _endIndex;

        public IAxisAccessor Gantry => new AxisAccessor(_log, Axis.GantryRtn, _startIndex, _endIndex);
        public IAxisAccessor Collimator => new AxisAccessor(_log, Axis.CollRtn, _startIndex, _endIndex);
        public IAxisAccessor CouchVrt => new AxisAccessor(_log, Axis.CouchVrt, _startIndex, _endIndex);
        public IAxisAccessor CouchLng => new AxisAccessor(_log, Axis.CouchLng, _startIndex, _endIndex);
        public IAxisAccessor CouchLat => new AxisAccessor(_log, Axis.CouchLat, _startIndex, _endIndex);
        public IAxisAccessor CouchRtn => new AxisAccessor(_log, Axis.CouchRtn, _startIndex, _endIndex);
        public IAxisAccessor CouchPitch => new AxisAccessor(_log, Axis.CouchPitch, _startIndex, _endIndex);
        public IAxisAccessor CouchRoll => new AxisAccessor(_log, Axis.CouchRoll, _startIndex, _endIndex);
        
        public IAxisAccessor X1 => new AxisAccessor(_log, Axis.X1, _startIndex, _endIndex);
        public IAxisAccessor X2 => new AxisAccessor(_log, Axis.X2, _startIndex, _endIndex);
        public IAxisAccessor Y1 => new AxisAccessor(_log, Axis.Y1, _startIndex, _endIndex);
        public IAxisAccessor Y2 => new AxisAccessor(_log, Axis.Y2, _startIndex, _endIndex);

        public IAxisAccessor MU => new AxisAccessor(_log, Axis.MU, _startIndex, _endIndex);
        public IAxisAccessor BeamHold => new AxisAccessor(_log, Axis.BeamHold, _startIndex, _endIndex);
        public IAxisAccessor ControlPoint => new AxisAccessor(_log, Axis.ControlPoint, _startIndex, _endIndex);

        public IAxisAccessor GetAxis(Axis axis)
        {
            if (axis == Axis.MLC)
                throw new ArgumentException("Use Mlc property for MLC axis access", nameof(axis));
            
            return new AxisAccessor(_log, axis, _startIndex, _endIndex);
        }

        private MlcAxisAccessor _mlc;
        public MlcAxisAccessor Mlc
        {
            get
            {
                if (_mlc == null)
                {
                    _mlc = CreateAllMlcAccessor();
                }
                return _mlc;
            }
        }

        private MlcAxisAccessor _movingMlc;
        public MlcAxisAccessor MovingMlc => _movingMlc ??= GetMovingMlc();

        public MlcAxisAccessor GetMovingMlc(float threshold = 0.001f)
        {
            var moving = new List<MlcLeafAxisAccessor>();
            var numLeaves = _log.Header.GetNumberOfLeafPairs();
            
            for (int bank = 0; bank < 2; bank++)
            {
                for (int leaf = 0; leaf < numLeaves; leaf++)
                {
                    if (IsLeafMoving(bank, leaf, threshold))
                    {
                        moving.Add(new MlcLeafAxisAccessor(_log, (Bank)bank, leaf, _startIndex, _endIndex));
                    }
                }
            }
            return new MlcAxisAccessor(_log, moving);
        }

        internal LogAxes(TrajectoryLog log, int startIndex, int endIndex)
        {
            _log = log;
            _startIndex = startIndex;
            _endIndex = endIndex;
        }

        private MlcAxisAccessor CreateAllMlcAccessor()
        {
            var leaves = new List<MlcLeafAxisAccessor>();
            var numLeaves = _log.Header.GetNumberOfLeafPairs();
            
            for (int bank = 0; bank < 2; bank++)
            {
                for (int leaf = 0; leaf < numLeaves; leaf++)
                {
                    leaves.Add(new MlcLeafAxisAccessor(_log, (Bank)bank, leaf, _startIndex, _endIndex));
                }
            }
            return new MlcAxisAccessor(_log, leaves);
        }

        private MlcAxisAccessor FindMovingMlcs()
        {
            return GetMovingMlc();
        }

        private bool IsLeafMoving(int bank, int leaf, float threshold)
        {
            if (_endIndex < _startIndex) return false;

            float firstVal = _log.GetMlcPosition(_startIndex, RecordType.ExpectedPosition, leaf, bank);
            
            for (int i = _startIndex + 1; i <= _endIndex; i++)
            {
                float val = _log.GetMlcPosition(i, RecordType.ExpectedPosition, leaf, bank);
                if (Math.Abs(val - firstVal) > threshold)
                    return true;
            }
            return false;
        }
    }
}