namespace TrajectoryLogReader.Log
{
    public class Header
    {
        /// <summary>
        /// Log-file version
        /// </summary>
        public double Version { get; internal set; }

        /// <summary>
        /// Sampling interval in milliseconds (typically 20ms).
        /// </summary>
        public int SamplingIntervalInMS { get; internal set; }

        /// <summary>
        /// Total number of <seealso cref="Axis"/> that exist in the file.
        /// </summary>
        public int NumAxesSampled { get; internal set; }

        /// <summary>
        /// The axes sampled.
        /// </summary>
        public Axis[] AxesSampled { get; internal set; }

        /// <summary>
        /// Number of samples per axis. Each entry in the array corresponds to one of the axes.
        /// For most axes it is 1, for MLCs it is the number of leaves and carriages.
        /// </summary>
        public int[] SamplesPerAxis { get; internal set; }

        /// <summary>
        /// Machine scale recorded in the log file
        /// </summary>
        public AxisScale AxisScale { get; internal set; }

        /// <summary>
        /// The number of sub-beams in the log.
        /// </summary>
        public int NumberOfSubBeams { get; internal set; }

        /// <summary>
        /// If the beam delivery exceeds the max number of snapshots (20 mins) the system stops recording.
        /// This is true if that was the case.
        /// </summary>
        public bool IsTruncated { get; internal set; }

        /// <summary>
        /// Number of snapshots recorded in the log file.
        /// </summary>
        public int NumberOfSnapshots { get; internal set; }

        /// <summary>
        /// The MLC model of the machine generating the log file.
        /// </summary>
        public MLCModel MlcModel { get; internal set; }

        /// <summary>
        /// Returns the number of samples for the particular axis index.
        /// If the axis isn't recorded, returns 0.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfSamples(int axisIndex)
        {
            if (axisIndex < 0 || axisIndex >= SamplesPerAxis.Length)
                return 0;
            return SamplesPerAxis[axisIndex];
        }

        /// <summary>
        /// Given an <see cref="Axis"/> returns the index that gives an indication of the order it was read from the log-file.
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        internal int GetAxisIndex(Axis axis)
        {
            return Array.IndexOf(AxesSampled, axis);
        }

        /// <summary>
        /// Returns the number of MLC leaf pairs for the machine producing the log-file.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfLeafPairs()
        {
            switch (MlcModel)
            {
                case MLCModel.NDS80:
                    return 40;
                case MLCModel.NDS120:
                case MLCModel.NDS120HD:
                    return 60;
                default:
                    return 0;
            }
        }
    }
}