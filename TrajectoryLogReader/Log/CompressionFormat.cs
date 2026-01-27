namespace TrajectoryLogReader.Log;

public enum CompressionFormat
{
    /// <summary>
    /// Standard file format
    /// </summary>
    Uncompressed,

    /// <summary>
    /// Lossy compression with deltas
    /// </summary>
    CompressedDelta,

    /// <summary>
    /// Lossy compression with deltas + gzip
    /// </summary>
    CompressedDeltaGZip
}