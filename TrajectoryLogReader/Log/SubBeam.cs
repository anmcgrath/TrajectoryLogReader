using TrajectoryLogReader.Complexity;
using TrajectoryLogReader.Fluence;
using TrajectoryLogReader.Log.Axes;
using TrajectoryLogReader.Log.Snapshots;
using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Log
{
    /// <summary>
    /// A sub-beam is created when a series of treatment fields are made automatic.
    /// </summary>
    public class SubBeam : ILogDataContainer
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
        public string Name { get; internal set; } = string.Empty;

        /// <summary>
        /// Accessor for axis data restricted to this sub-beam.
        /// </summary>
        public LogAxes Axes => field ??= new LogAxes(_log, StartIndex, EndIndex);

        /// <summary>
        /// The index of the snapshot in the log-file that corresponds to the start of this beam
        /// </summary>
        public int StartIndex
        {
            get
            {
                if (field < 0)
                    field = CalculateStartIndex();
                return field;
            }
        } = -1;

        /// <summary>
        /// The index of the snapshot in the log-file that corresponds to the end of this beam
        /// </summary>
        public int EndIndex
        {
            get
            {
                if (field < 0)
                    field = CalculateEndIndex();
                return field;
            }
        } = -1;

        /// <summary>
        /// A collection of measurement snapshots specific to this sub-beam.
        /// </summary>
        public SnapshotCollection Snapshots
        {
            get
            {
                field ??= new SnapshotCollection(_log, StartIndex, EndIndex);
                return field;
            }
        }

        /// <summary>
        /// Calculated statistics for this sub-beam.
        /// </summary>
        public Statistics Statistics
        {
            get
            {
                field ??= new Statistics(Snapshots, _log);
                return field;
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

        /// <summary>
        /// Calculates the Average Leaf Pair Opening (ALPO) for this sub-beam.
        /// ALPO measures the average gap between opposing MLC leaves for leaf pairs within the jaw opening.
        /// </summary>
        /// <param name="options">Calculation options. If null, default options are used.</param>
        /// <returns>The average leaf pair opening in centimeters.</returns>
        public double CalculateAverageLeafPairOpening(AverageLeafPairOpeningOptions? options = null)
        {
            return AverageLeafPairOpeningCalculator.Calculate(Snapshots, options);
        }

        private int CalculateStartIndex()
        {
            var cpData = _log.GetAxisData(Axis.ControlPoint);
            var stride = cpData.SamplesPerSnapshot;

            for (int i = 0; i < cpData.NumSnapshots; i++)
            {
                var cp = cpData.Data[i * stride + 0];
                if (cp > ControlPoint) // beam ends at same cp
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