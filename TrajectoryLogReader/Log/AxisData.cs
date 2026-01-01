namespace TrajectoryLogReader.Log
{
    public class AxisData
    {
        /// <summary>
        /// The number of snapshots
        /// </summary>
        public int NumSnapshots { get; internal set; }

        /// <summary>
        /// Flattened data array.
        /// Layout: [Snapshot0_Sample0, Snapshot0_Sample1, ..., Snapshot1_Sample0, ...]
        /// </summary>
        internal float[] Data { get; set; }

        /// <summary>
        /// Number of samples per snapshot (Stride).
        /// </summary>
        public int SamplesPerSnapshot { get; internal set; }

        internal AxisData(int numberOfSnapshots, int samplesPerSnapshot)
        {
            NumSnapshots = numberOfSnapshots;
            SamplesPerSnapshot = samplesPerSnapshot;
            Data = new float[numberOfSnapshots * samplesPerSnapshot];
        }
    }
}