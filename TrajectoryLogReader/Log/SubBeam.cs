using TrajectoryLogReader.Fluence;
using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Log
{
    /// <summary>
    /// A sub-beam is created when a series of treatment fields are made automatic.
    /// </summary>
    public class SubBeam
    {
        private readonly TrajectoryLog _log;

        /// <summary>
        /// The control point index of the start of this sub-beam
        /// </summary>
        public int ControlPoint { get; internal set; }

        /// <summary>
        /// The total MU delivered in the sub-beam.
        /// </summary>
        public float MU { get; internal set; }

        /// <summary>
        /// Total expected radiation time in seconds
        /// </summary>
        public float RadTime { get; internal set; }

        /// <summary>
        /// Sequence number of the sub-beam
        /// </summary>
        public int SequenceNumber { get; internal set; }

        /// <summary>
        /// The name of the sub-beam.
        /// </summary>
        public string Name { get; internal set; }

        private int _startIndex = -1;

        /// <summary>
        /// The index of the snapshot in the log-file that corresponds to the start of this beam
        /// </summary>
        public int StartIndex
        {
            get
            {
                if (_startIndex < 0)
                    _startIndex = CalculateStartIndex();
                return _startIndex;
            }
        }

        private int _endIndex = -1;

        /// <summary>
        /// The index of the snapshot in the log-file that corresponds to the end of this beam
        /// </summary>
        public int EndIndex
        {
            get
            {
                if (_endIndex < 0)
                    _endIndex = CalculateEndIndex();
                return _endIndex;
            }
        }

        private MeasurementDataCollection _snapshots;

        /// <summary>
        /// A collection of measurement snapshots specific to this sub-beam.
        /// </summary>
        public MeasurementDataCollection Snapshots
        {
            get
            {
                if (_snapshots == null)
                    _snapshots = new MeasurementDataCollection(_log, StartIndex, EndIndex);
                return _snapshots;
            }
        }

        private Statistics _statistics;

        /// <summary>
        /// Calculated statistics for this sub-beam.
        /// </summary>
        public Statistics Statistics
        {
            get
            {
                if (_statistics == null)
                    _statistics = new Statistics(Snapshots, _log);
                return _statistics;
            }
        }

        private readonly FluenceCreator _fluenceCreator = new();

        /// <summary>
        /// Create a <see cref="FieldFluence"/> object for the beam
        /// </summary>
        /// <param name="options"></param>
        /// <param name="recordType"></param>
        /// <param name="samplingRateInMs">Determines how often we sample the log file for fluence data. Default is 20 seconds which is every measurement snapshot. This should be a multiple of the log file sampling rate</param>
        /// <returns></returns>
        public FieldFluence CreateFluence(FluenceOptions options, RecordType recordType, double samplingRateInMs = 20)
        {
            return _fluenceCreator.Create(options, recordType, samplingRateInMs, Snapshots);
        }


        internal SubBeam(TrajectoryLog log)
        {
            _log = log;
        }

        private int CalculateStartIndex()
        {
            var cpData = _log.GetAxisData(Axis.ControlPoint);
            var stride = cpData.SamplesPerSnapshot;

            for (int i = 0; i < cpData.NumSnapshots; i++)
            {
                var cp = cpData.Data[i * stride + 0];
                if (cp >= ControlPoint)
                    return i;
            }

            return -2;
        }

        private int CalculateEndIndex()
        {
            if (StartIndex == -2)
                return StartIndex;

            var nextBeam = _log
                .SubBeams
                .OrderBy(x => x.SequenceNumber)
                .FirstOrDefault(x => x.SequenceNumber > SequenceNumber);

            if (nextBeam == null)
                return _log.Header.NumberOfSnapshots - 1;

            if (nextBeam.StartIndex == -2) // beam has not started
                return _log.Header.NumberOfSnapshots - 1;

            return nextBeam.StartIndex - 1;
        }
    }
}