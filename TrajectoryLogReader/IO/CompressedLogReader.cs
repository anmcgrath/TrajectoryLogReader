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
            // GZip compressed - decompress first
            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
            using var decompressed = new MemoryStream();
            gzipStream.CopyTo(decompressed);
            decompressed.Position = 0;
            var log = ReadFromStream(decompressed);
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
    /// Used for MLC, jaws, small tilt angles (pitch/roll).
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

        short firstQuantized = br.ReadInt16();
        float firstValue = DequantizeFromShort(firstQuantized, scale);
        axisData.Data[sampleOffset] = firstValue;

        short previousQuantized = firstQuantized;

        for (int snapshot = 1; snapshot < numSnapshots; snapshot++)
        {
            int dataIndex = snapshot * stride + sampleOffset;

            sbyte deltaOrEscape = br.ReadSByte();

            short currentQuantized;
            if (deltaOrEscape == EscapeCode8)
            {
                currentQuantized = br.ReadInt16();
            }
            else
            {
                int delta = deltaOrEscape;
                currentQuantized = (short)(previousQuantized + delta);
            }

            axisData.Data[dataIndex] = DequantizeFromShort(currentQuantized, scale);
            previousQuantized = currentQuantized;
        }
    }

    /// <summary>
    /// Reads a stream using 32-bit base values and 16-bit deltas.
    /// Used for couch positions, MU, ControlPoint, and full-rotation angles.
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

        int firstQuantized = br.ReadInt32();
        float firstValue = DequantizeFromInt(firstQuantized, scale);
        axisData.Data[sampleOffset] = firstValue;

        int previousQuantized = firstQuantized;

        for (int snapshot = 1; snapshot < numSnapshots; snapshot++)
        {
            int dataIndex = snapshot * stride + sampleOffset;

            short deltaOrEscape = br.ReadInt16();

            int currentQuantized;
            if (deltaOrEscape == EscapeCode16)
            {
                currentQuantized = br.ReadInt32();
            }
            else
            {
                currentQuantized = previousQuantized + deltaOrEscape;
            }

            axisData.Data[dataIndex] = DequantizeFromInt(currentQuantized, scale);
            previousQuantized = currentQuantized;
        }
    }

    private static float DequantizeFromShort(short quantized, float scale)
    {
        return quantized / scale;
    }

    private static float DequantizeFromInt(int quantized, float scale)
    {
        return quantized / scale;
    }
}