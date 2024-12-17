namespace TrajectoryLogReader.Log
{
    /// <summary>
    /// A sub-beam is created when a series of treatment fields are made automatic.
    /// </summary>
    public class SubBeam
    {
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
    }
}