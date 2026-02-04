using System.Buffers;
using System.IO.Compression;
using System.Text;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.IO;

/// <summary>
/// Reads a compressed trajectory log file and decompresses it to a TrajectoryLog.
/// </summary>
public static class CompressedLogReader
{
    /// <summary>
    /// Size of the file signature block in bytes.
    /// </summary>
    private const int SignatureSize = 16;

    /// <summary>
    /// Expected file signature for compressed trajectory log files.
    /// </summary>
    private const string ExpectedSignature = "VOSTLC";

    /// <summary>
    /// Maximum allowed file size (100 MB - compressed files should be smaller).
    /// </summary>
    private const long MaxAllowedFileSize = 100 * 1024 * 1024;

    /// <summary>
    /// Escape code for 8-bit delta streams.
    /// </summary>
    private const sbyte EscapeCode8 = -128; // 0x80

    /// <summary>
    /// Escape code for 16-bit delta streams.
    /// </summary>
    private const short EscapeCode16 = short.MinValue; // 0x8000

    /// <summary>
    /// Axes that require 32-bit storage.
    /// </summary>
    private static readonly HashSet<Axis> LargeValueAxes = new()
    {
        Axis.CouchVrt,
        Axis.CouchLng,
        Axis.CouchLat,
        Axis.MU,
        Axis.ControlPoint,
        // Full rotation angles need 32-bit: 360° * 100 = 36000 > short.MaxValue (32767)
        Axis.GantryRtn,
        Axis.CollRtn,
        Axis.CouchRtn
    };

    /// <summary>
    /// GZip magic bytes for auto-detection.
    /// </summary>
    private const byte GzipMagic1 = 0x1F;

    private const byte GzipMagic2 = 0x8B;

    /// <summary>
    /// Reads a compressed trajectory log file.
    /// Automatically detects and handles GZip compression.
    /// </summary>
    public static TrajectoryLog Read(string filePath)
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Compressed trajectory log file not found.", filePath);

        if (fileInfo.Length > MaxAllowedFileSize)
            throw new InvalidDataException(
                $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({MaxAllowedFileSize} bytes).");

        using var fs = File.OpenRead(filePath);
        var log = Read(fs);
        log.FilePath = filePath;
        return log;
    }

    /// <summary>
    /// Reads a compressed trajectory log from a stream.
    /// Automatically detects and handles GZip compression.
    /// </summary>
    public static TrajectoryLog Read(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        // Check for GZip magic bytes
        var magic = new byte[2];
        var bytesRead = stream.Read(magic, 0, 2);
        if (bytesRead < 2)
            throw new EndOfStreamException("File too short to determine format.");

        // Reset stream position
        stream.Position = 0;

        if (magic[0] == GzipMagic1 && magic[1] == GzipMagic2)
        {
            // GZip compressed - decompress using pooled buffers
            var log = DecompressAndRead(stream);
            log.CompressionFormat = CompressionFormat.CompressedDeltaGZip;
            return log;
        }
        else
        {
            var log = ReadFromStream(stream);
            log.CompressionFormat = CompressionFormat.CompressedDelta;
            return log;
        }
    }

    /// <summary>
    /// Decompresses GZip stream using pooled buffers to reduce allocations.
    /// </summary>
    private static TrajectoryLog DecompressAndRead(Stream compressedStream)
    {
        const int initialBufferSize = 64 * 1024; // 64KB initial
        const int chunkSize = 32 * 1024; // 32KB chunks

        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveOpen: true);

        // Use a pooled buffer for reading
        byte[] buffer = ArrayPool<byte>.Shared.Rent(initialBufferSize);
        int totalBytesRead = 0;

        try
        {
            while (true)
            {
                // Ensure we have space for the next chunk
                if (totalBytesRead + chunkSize > buffer.Length)
                {
                    // Need to grow - rent a larger buffer
                    byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    Buffer.BlockCopy(buffer, 0, newBuffer, 0, totalBytesRead);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }

                int bytesRead = gzipStream.Read(buffer, totalBytesRead, chunkSize);
                if (bytesRead == 0)
                    break;

                totalBytesRead += bytesRead;
            }

            // Create a MemoryStream over the exact data (no copy, just wraps the buffer)
            using var ms = new MemoryStream(buffer, 0, totalBytesRead, writable: false);
            return ReadFromStream(ms);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static TrajectoryLog ReadFromStream(Stream stream)
    {
        var utf8 = Encoding.UTF8;
        var log = new TrajectoryLog() { FilePath = null };
        var header = new Header();

        using var br = new BinaryReader(stream, Encoding.UTF8, true);

        // Read and validate signature
        var sigBytes = br.ReadBytes(SignatureSize);
        if (sigBytes.Length < SignatureSize)
            throw new EndOfStreamException($"File too short: expected at least {SignatureSize} bytes for signature.");

        var sig = utf8.GetString(sigBytes);
        if (!sig.StartsWith(ExpectedSignature))
            throw new InvalidDataException(
                $"Invalid file signature '{sig.TrimEnd('\0')}'. Expected '{ExpectedSignature}'.");

        // Read format version (for future compatibility)
        var formatVersionBytes = br.ReadBytes(SignatureSize);
        // var formatVersion = utf8.GetString(formatVersionBytes).TrimEnd('\0');

        // Read original log version
        header.Version = br.ReadDouble();

        // Read header
        header.SamplingIntervalInMS = br.ReadInt32();
        header.NumAxesSampled = br.ReadInt32();

        if (header.NumAxesSampled < 0 || header.NumAxesSampled > 1000)
            throw new InvalidDataException($"Invalid number of axes sampled: {header.NumAxesSampled}.");

        header.AxesSampled = new Axis[header.NumAxesSampled];
        for (int i = 0; i < header.NumAxesSampled; i++)
            header.AxesSampled[i] = (Axis)br.ReadInt32();

        header.SamplesPerAxis = new int[header.NumAxesSampled];
        for (int i = 0; i < header.NumAxesSampled; i++)
            header.SamplesPerAxis[i] = br.ReadInt32();

        header.AxisScale = (AxisScale)br.ReadInt32();
        header.NumberOfSubBeams = br.ReadInt32();

        if (header.NumberOfSubBeams < 0 || header.NumberOfSubBeams > 10000)
            throw new InvalidDataException($"Invalid number of sub-beams: {header.NumberOfSubBeams}.");

        header.IsTruncated = br.ReadInt32() == 1;
        header.NumberOfSnapshots = br.ReadInt32();

        if (header.NumberOfSnapshots < 0 || header.NumberOfSnapshots > 10_000_000)
            throw new InvalidDataException($"Invalid number of snapshots: {header.NumberOfSnapshots}.");

        header.MlcModel = (MLCModel)br.ReadInt32();

        log.MlcModel = header.MlcModel == MLCModel.NDS120 ? new Millenium120MLC() : null;
        log.Header = header;

        // Read metadata
        log.MetaData = LogIOHelper.ReadMetaData(br.ReadBytes(LogIOHelper.MetaDataSize));

        // Read subbeams
        for (int i = 0; i < header.NumberOfSubBeams; i++)
        {
            log.SubBeams.Add(LogIOHelper.ReadSubBeam(br, log));
        }

        // Allocate axis data
        log.AxisData = new AxisData[header.NumAxesSampled];
        for (int i = 0; i < header.NumAxesSampled; i++)
        {
            var samplesForAxis = header.GetNumberOfSamples(i) * 2;
            log.AxisData[i] = new AxisData(header.NumberOfSnapshots, samplesForAxis);
        }

        // Read compressed axis data
        ReadCompressedAxisData(br, log);

        return log;
    }

    /// <summary>
    /// Reads a compressed trajectory log file asynchronously.
    /// </summary>
    public static async Task<TrajectoryLog> ReadAsync(string filePath)
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Compressed trajectory log file not found.", filePath);

        if (fileInfo.Length > MaxAllowedFileSize)
            throw new InvalidDataException(
                $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({MaxAllowedFileSize} bytes).");

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        using var ms = new MemoryStream();
        await fs.CopyToAsync(ms);
        ms.Position = 0;

        var log = Read(ms);
        log.FilePath = filePath;
        return log;
    }

    private static void ReadCompressedAxisData(BinaryReader br, TrajectoryLog log)
    {
        var header = log.Header;

        // Read scale table
        int scaleCount = br.ReadInt32();
        var scales = new float[scaleCount];
        for (int i = 0; i < scaleCount; i++)
            scales[i] = br.ReadSingle();

        // Read axis data using per-stream scales
        int scaleIndex = 0;
        for (int axisIndex = 0; axisIndex < header.NumAxesSampled; axisIndex++)
        {
            var axis = header.AxesSampled[axisIndex];
            var axisData = log.AxisData[axisIndex];
            var samplesPerSnapshot = axisData.SamplesPerSnapshot;
            var isLargeValue = LargeValueAxes.Contains(axis);

            for (int sampleOffset = 0; sampleOffset < samplesPerSnapshot; sampleOffset++)
            {
                var scale = scales[scaleIndex++];

                if (isLargeValue)
                {
                    ReadLargeValueStream(br, axisData, sampleOffset, header.NumberOfSnapshots,
                        samplesPerSnapshot, scale);
                }
                else
                {
                    ReadSmallValueStream(br, axisData, sampleOffset, header.NumberOfSnapshots,
                        samplesPerSnapshot, scale);
                }
            }
        }
    }

    /// <summary>
    /// Reads a stream using 16-bit base values and 8-bit deltas.
    /// Uses batch reading to reduce per-value syscall overhead.
    /// </summary>
    private static void ReadSmallValueStream(
        BinaryReader br,
        AxisData axisData,
        int sampleOffset,
        int numSnapshots,
        int stride,
        float scale)
    {
        if (numSnapshots == 0) return;

        // Read base value
        short firstQuantized = br.ReadInt16();
        float invScale = 1.0f / scale;
        axisData.Data[sampleOffset] = firstQuantized * invScale;

        if (numSnapshots == 1) return;

        short previousQuantized = firstQuantized;
        int deltasToRead = numSnapshots - 1;

        // Worst case: each delta is escape (1 byte) + absolute (2 bytes) = 3 bytes per snapshot
        // Best case: 1 byte per delta
        // Read in batches to reduce syscall overhead while handling escapes
        const int batchSize = 4096;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(batchSize);

        try
        {
            int snapshot = 1;
            int bufferPos = 0;
            int bufferLen = 0;

            while (snapshot < numSnapshots)
            {
                // Refill buffer if needed
                if (bufferPos >= bufferLen)
                {
                    bufferLen = br.Read(buffer, 0, batchSize);
                    bufferPos = 0;
                    if (bufferLen == 0)
                        throw new EndOfStreamException("Unexpected end of stream reading small value deltas.");
                }

                sbyte deltaOrEscape = (sbyte)buffer[bufferPos++];

                short currentQuantized;
                if (deltaOrEscape == EscapeCode8)
                {
                    // Need 2 more bytes for absolute value
                    if (bufferPos + 2 > bufferLen)
                    {
                        // Push back the bytes we need and refill
                        int remaining = bufferLen - bufferPos;
                        if (remaining > 0)
                            Buffer.BlockCopy(buffer, bufferPos, buffer, 0, remaining);
                        int additionalRead = br.Read(buffer, remaining, batchSize - remaining);
                        bufferLen = remaining + additionalRead;
                        bufferPos = 0;
                        if (bufferLen < 2)
                            throw new EndOfStreamException("Unexpected end of stream reading absolute value.");
                    }
                    currentQuantized = (short)(buffer[bufferPos] | (buffer[bufferPos + 1] << 8));
                    bufferPos += 2;
                }
                else
                {
                    currentQuantized = (short)(previousQuantized + deltaOrEscape);
                }

                int dataIndex = snapshot * stride + sampleOffset;
                axisData.Data[dataIndex] = currentQuantized * invScale;
                previousQuantized = currentQuantized;
                snapshot++;
            }

            // If we read more than needed, seek back
            int unusedBytes = bufferLen - bufferPos;
            if (unusedBytes > 0 && br.BaseStream.CanSeek)
            {
                br.BaseStream.Seek(-unusedBytes, SeekOrigin.Current);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Reads a stream using 32-bit base values and 16-bit deltas.
    /// Uses batch reading to reduce per-value syscall overhead.
    /// </summary>
    private static void ReadLargeValueStream(
        BinaryReader br,
        AxisData axisData,
        int sampleOffset,
        int numSnapshots,
        int stride,
        float scale)
    {
        if (numSnapshots == 0) return;

        // Read base value
        int firstQuantized = br.ReadInt32();
        float invScale = 1.0f / scale;
        axisData.Data[sampleOffset] = firstQuantized * invScale;

        if (numSnapshots == 1) return;

        int previousQuantized = firstQuantized;

        // Worst case: each delta is escape (2 bytes) + absolute (4 bytes) = 6 bytes per snapshot
        // Best case: 2 bytes per delta
        const int batchSize = 4096;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(batchSize);

        try
        {
            int snapshot = 1;
            int bufferPos = 0;
            int bufferLen = 0;

            while (snapshot < numSnapshots)
            {
                // Refill buffer if needed (need at least 2 bytes for delta)
                if (bufferPos + 2 > bufferLen)
                {
                    int remaining = bufferLen - bufferPos;
                    if (remaining > 0)
                        Buffer.BlockCopy(buffer, bufferPos, buffer, 0, remaining);
                    int additionalRead = br.Read(buffer, remaining, batchSize - remaining);
                    bufferLen = remaining + additionalRead;
                    bufferPos = 0;
                    if (bufferLen < 2)
                        throw new EndOfStreamException("Unexpected end of stream reading large value deltas.");
                }

                short deltaOrEscape = (short)(buffer[bufferPos] | (buffer[bufferPos + 1] << 8));
                bufferPos += 2;

                int currentQuantized;
                if (deltaOrEscape == EscapeCode16)
                {
                    // Need 4 more bytes for absolute value
                    if (bufferPos + 4 > bufferLen)
                    {
                        int remaining = bufferLen - bufferPos;
                        if (remaining > 0)
                            Buffer.BlockCopy(buffer, bufferPos, buffer, 0, remaining);
                        int additionalRead = br.Read(buffer, remaining, batchSize - remaining);
                        bufferLen = remaining + additionalRead;
                        bufferPos = 0;
                        if (bufferLen < 4)
                            throw new EndOfStreamException("Unexpected end of stream reading absolute value.");
                    }
                    currentQuantized = buffer[bufferPos] | (buffer[bufferPos + 1] << 8) |
                                       (buffer[bufferPos + 2] << 16) | (buffer[bufferPos + 3] << 24);
                    bufferPos += 4;
                }
                else
                {
                    currentQuantized = previousQuantized + deltaOrEscape;
                }

                int dataIndex = snapshot * stride + sampleOffset;
                axisData.Data[dataIndex] = currentQuantized * invScale;
                previousQuantized = currentQuantized;
                snapshot++;
            }

            // If we read more than needed, seek back
            int unusedBytes = bufferLen - bufferPos;
            if (unusedBytes > 0 && br.BaseStream.CanSeek)
            {
                br.BaseStream.Seek(-unusedBytes, SeekOrigin.Current);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}