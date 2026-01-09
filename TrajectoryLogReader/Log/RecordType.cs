namespace TrajectoryLogReader.Log
{
    /// <summary>
    /// Specifies the type of position record (Expected or Actual).
    /// </summary>
    public enum RecordType
    {
        /// <summary>
        /// Record is expected position sent to the linac
        /// </summary>
        ExpectedPosition,
        /// <summary>
        /// Record is the actual position of the axis data
        /// </summary>
        ActualPosition
    }
}