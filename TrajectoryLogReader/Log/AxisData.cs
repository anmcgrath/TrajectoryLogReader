namespace TrajectoryLogReader.Log
{
    public class AxisData
    {
        /// <summary>
        /// The number of snapshots
        /// </summary>
        public int NumSnapshots { get; internal set; }

        /// <summary>
        /// An array of snapshot data. The length of this array is equal to the number of snapshots.
        /// The length of each array depends on the number of samples per axis.
        /// For most axes it is two - the first value is expected, the second is actual.
        /// For MLC axis it is equal to the (number of leaves + 2 (num carriages)) * 2.
        /// For MLC, the first two pairs are carriage A and carriage B positions.
        /// Then, each alternating pair gives the
        /// </summary>
        public float[][] RawData { get; internal set; }

        internal AxisData(int numberOfSnapshots)
        {
            NumSnapshots = numberOfSnapshots;
            RawData = new float[numberOfSnapshots][];
        }
    }
}