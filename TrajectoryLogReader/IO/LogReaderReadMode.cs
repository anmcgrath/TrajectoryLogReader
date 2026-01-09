namespace TrajectoryLogReader.IO
{
    /// <summary>
    /// Specifies which parts of the log file to read.
    /// </summary>
    public enum LogReaderReadMode
    {
        /// <summary>
        /// Read everything, including all measurement snapshots.
        /// </summary>
        All,
        /// <summary>
        /// Read only the header and metadata, skipping measurement data.
        /// </summary>
        HeaderAndMetaData
    }
}