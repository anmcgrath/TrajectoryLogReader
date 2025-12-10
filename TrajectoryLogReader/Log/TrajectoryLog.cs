using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log
{
    public class TrajectoryLog
    {
        /// <summary>
        /// Trajectory log header information
        /// </summary>
        public Header Header { get; internal set; }

        /// <summary>
        /// Trajectory log meta data information
        /// </summary>
        public MetaData MetaData { get; internal set; }

        /// <summary>
        /// List of <seealso cref="SubBeams"/> in the log.
        /// </summary>
        public List<SubBeam> SubBeams { get; internal set; } = new List<SubBeam>();

        /// <summary>
        /// Raw axis data.
        /// </summary>
        internal AxisData[] AxisData { get; set; }

        /// <summary>
        /// The MLC model used on the machine recording the log file.
        /// </summary>
        public IMLCModel MlcModel { get; internal set; }

        /// <summary>
        /// The file path to the log
        /// </summary>
        public string FilePath { get; internal set; }

        /// <summary>
        /// The total time (in ms) of the trajectory log recording.
        /// </summary>
        public double TotalTimeInMs => Header.SamplingIntervalInMS * Header.NumberOfSnapshots;

        public MeasurementDataCollection Snapshots { get; }

        internal TrajectoryLog()
        {
            Snapshots = new MeasurementDataCollection(this);
        }

        /// <summary>
        /// Interpolates axis data given a time in MS.
        /// </summary>
        /// <param name="axis">The axis data to interpolate</param>
        /// <param name="timeInMs">The time of the sample</param>
        /// <param name="offset">
        /// The integer offset for the data sampled.
        /// For most axes it should be either 0 (expected) or 1 (actual).
        /// For MLCs the offset takes into account the MLC carriage/leaf positions as well
        /// as if it's expected or actual. For MLCs, use <see cref="InterpolateMLCPosition"/>
        /// </param>
        /// <returns></returns>
        public float InterpolateAxisData(Axis axis, double timeInMs, int offset)
        {
            if (timeInMs > TotalTimeInMs)
                throw new Exception($"Time {timeInMs} is greater than {TotalTimeInMs}");

            var cpIndex = timeInMs / Header.SamplingIntervalInMS;
            var i0 = (int)cpIndex;
            var i1 = i0 + 1;

            if (i1 >= Header.NumberOfSnapshots)
            {
                i1 = Header.NumberOfSnapshots - 1;
                i0 = i1 - 1;
            }

            var t0 = i0 * Header.SamplingIntervalInMS;
            var t1 = i1 * Header.SamplingIntervalInMS;

            var axisIndex = Header.GetAxisIndex(axis);
            if (axis < 0 || axisIndex > AxisData.Length - 1)
                throw new Exception($"Data for axis {axis} does not exist.");

            var v0 = AxisData[axisIndex].RawData[i0][offset];
            var v1 = AxisData[axisIndex].RawData[i1][offset];

            var f = (timeInMs - t0) / (t1 - t0);
            return (float)(v0 + (v1 - v0) * f);
        }

        public float GetAxisData(Axis axis, int measIndex, RecordType recordType) =>
            GetAxisData(axis, measIndex, offset: recordType == RecordType.ActualPosition ? 1 : 0);

        public float GetAxisData(Axis axis, int measIndex, int offset)
        {
            var axisIndex = Header.GetAxisIndex(axis);
            return AxisData[axisIndex].RawData[measIndex][offset];
        }

        public float InterpolateAxisData(Axis axis, double timeInMs, RecordType recordType) =>
            InterpolateAxisData(axis, timeInMs, offset: recordType == RecordType.ActualPosition ? 1 : 0);

        /// <summary>
        /// Returns the MLC position (in cm) at the specified time <paramref name="timeInMs"/>
        /// </summary>
        /// <param name="timeInMs">The time of the sample</param>
        /// <param name="bank">The bank position. Bank A = 0, Bank B = 1</param>
        /// <param name="leafIndex">The leaf index.</param>
        /// <param name="recordType"></param>
        /// <returns></returns>
        public float InterpolateMLCPosition(double timeInMs, int bank, int leafIndex, RecordType recordType)
        {
            var numLeaves = Header.GetNumberOfLeafPairs();
            var offset = (bank * numLeaves * 2 + leafIndex * 2) + (recordType == RecordType.ActualPosition ? 1 : 0) + 4;
            return InterpolateAxisData(Axis.MLC, timeInMs, offset);
        }

        /// <summary>
        /// Returns the MLC position (in cm) at the specified time <paramref name="timeInMs"/>
        /// </summary>
        /// <param name="measIndex">The measurement index 0.. number of data points - 1</param>
        /// <param name="recordType"></param>
        /// <returns></returns>
        internal float[,] GetMlcPositions(int measIndex, RecordType recordType)
        {
            var numLeaves = Header.GetNumberOfLeafPairs();
            var data = new float[2, numLeaves];
            for (int bank = 0; bank < 2; bank++)
            {
                for (int leafIndex = 0; leafIndex < numLeaves; leafIndex++)
                {
                    var offset = (bank * numLeaves * 2 + leafIndex * 2) +
                                 (recordType == RecordType.ActualPosition ? 1 : 0) + 4;
                    data[1 - bank, leafIndex] = GetAxisData(Axis.MLC, measIndex, leafIndex);
                }
            }

            return data;
        }

        /// <summary>
        /// Returns the time (in ms) at the control point index specified.
        /// The control point index <paramref name="fractionalCp"/> can be fractional, since the log samples the machine at a high rate.
        /// </summary>
        /// <param name="fractionalCp"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int GetTimeInMsAtControlPoint(double fractionalCp)
        {
            var cpAxisIndex = Header.GetAxisIndex(Axis.ControlPoint);
            if (cpAxisIndex < 0)
                throw new Exception("No control point data found.");

            var cpData = AxisData[cpAxisIndex];
            for (int i = 0; i < cpData.NumSnapshots; i++)
            {
                var cp = cpData.RawData[i][0]; // expected and actual are the same for CP
                if (cp >= fractionalCp)
                {
                    var nextCpFrac = cpData.RawData[i + 1][0];
                    var f = (nextCpFrac - cp == 0) ? 0 : (fractionalCp - cp) / (cpData.RawData[i + 1][0] - cp);
                    return (int)(i * Header.SamplingIntervalInMS + f * Header.SamplingIntervalInMS);
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns a 2D array of interpolated MLC positions (in cm) at time <paramref name="timeInMs"/> of the form mlc[bankIndex, leafIndex]
        /// </summary>
        /// <param name="timeInMs"></param>
        /// <param name="recordType"></param>
        /// <returns></returns>
        public float[,] InterpolateMLCPositions(int timeInMs, RecordType recordType)
        {
            var numLeaves = Header.GetNumberOfLeafPairs();
            float[,] mlc = new float[2, numLeaves];

            for (int bank = 0; bank < 2; bank++)
            {
                for (int leafIndex = 0; leafIndex < numLeaves; leafIndex++)
                {
                    var mlcPos = InterpolateMLCPosition(timeInMs, bank, leafIndex, recordType) * 10;
                    if (bank == 1)
                        mlcPos = -mlcPos;

                    mlc[1 - bank, leafIndex] = mlcPos;
                }
            }

            return mlc;
        }

        /// <summary>
        /// Returns the total number of control points recorded in the log file.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfControlPoints()
        {
            var cpAxisIndex = Header.GetAxisIndex(Axis.ControlPoint);
            if (cpAxisIndex < 0)
                return 0;
            var axisData = AxisData[cpAxisIndex];
            return (int)Math.Round(axisData.RawData.Last()[0], 0) + 1;
        }

        /// <summary>
        /// Returns the entire <see cref="Log.AxisData"/> for the given <paramref name="axis"/>
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public AxisData GetAxisData(Axis axis)
        {
            var axisIndex = Header.GetAxisIndex(axis);
            return AxisData[axisIndex];
        }
    }
}