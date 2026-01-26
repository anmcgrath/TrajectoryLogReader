using System.Text;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.IO;

public static class LogReader
{
    /// <summary>
    /// Size of the file signature block in bytes.
    /// </summary>
    private const int SignatureSize = 16;

    /// <summary>
    /// Size of the header block in bytes (fixed by TrueBeam spec).
    /// </summary>
    private const int HeaderSize = 1024;

    /// <summary>
    /// Size of the metadata block in bytes.
    /// </summary>
    private const int MetaDataSize = LogIOHelper.MetaDataSize;

    /// <summary>
    /// Maximum allowed file size (500 MB)
    /// </summary>
    private const long MaxAllowedFileSize = 500 * 1024 * 1024;

    /// <summary>
    /// Expected file signature prefix for Varian trajectory log files.
    /// </summary>
    private const string ExpectedSignature = "VOSTL";

    /// <summary>
    /// Expected file signature prefix for compressed trajectory log files.
    /// </summary>
    private const string CompressedSignature = "VOSTLC";

    /// <summary>
    /// GZip magic bytes for auto-detection.
    /// </summary>
    private const byte GzipMagic1 = 0x1F;
    private const byte GzipMagic2 = 0x8B;

    /// <summary>
    /// Reads a trajectory log file (Varian) asynchronously.
    /// Automatically detects compressed (.cbin) vs uncompressed (.bin) format.
    /// </summary>
    /// <param name="filePath">The log file path</param>
    /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata.</param>
    /// <returns>The parsed trajectory log.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file format is invalid.</exception>
    public static async Task<TrajectoryLog> ReadBinaryAsync(string filePath,
        LogReaderReadMode mode = LogReaderReadMode.All)
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Trajectory log file not found.", filePath);

        if (fileInfo.Length > MaxAllowedFileSize)
            throw new InvalidDataException(
                $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({MaxAllowedFileSize} bytes).");

        // Check if compressed format
        if (await IsCompressedFormatAsync(filePath))
        {
            return await CompressedLogReader.ReadAsync(filePath);
        }

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var log = await ReadBinaryAsync(fs, mode);
        log.FilePath = filePath;
        return log;
    }

    /// <summary>
    /// Reads a trajectory log file (Varian).
    /// Automatically detects compressed (.cbin) vs uncompressed (.bin) format.
    /// </summary>
    /// <param name="filePath">The log file path</param>
    /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata.</param>
    /// <returns>The parsed trajectory log.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file format is invalid.</exception>
    public static TrajectoryLog ReadBinary(string filePath, LogReaderReadMode mode = LogReaderReadMode.All)
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Trajectory log file not found.", filePath);

        if (fileInfo.Length > MaxAllowedFileSize)
            throw new InvalidDataException(
                $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({MaxAllowedFileSize} bytes).");

        // Check if compressed format
        if (IsCompressedFormat(filePath))
        {
            return CompressedLogReader.Read(filePath);
        }

        using var fs = File.OpenRead(filePath);
        var log = ReadBinary(fs, mode);
        log.FilePath = filePath;
        return log;
    }

    /// <summary>
    /// Reads a trajectory log file (Varian) from a stream.
    /// Automatically detects compressed vs uncompressed format.
    /// </summary>
    /// <param name="stream">The stream to read from (must be seekable).</param>
    /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata.</param>
    /// <returns>The parsed trajectory log.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file format is invalid.</exception>
    public static TrajectoryLog ReadBinary(Stream stream, LogReaderReadMode mode = LogReaderReadMode.All)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        // Check if compressed format (resets stream position)
        if (IsCompressedFormat(stream))
        {
            return CompressedLogReader.Read(stream);
        }

        return ParseFromStream(stream, mode);
    }

    /// <summary>
    /// Reads a trajectory log file (Varian) from a stream asynchronously.
    /// Automatically detects compressed vs uncompressed format.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata.</param>
    /// <returns>The parsed trajectory log.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file format is invalid.</exception>
    public static async Task<TrajectoryLog> ReadBinaryAsync(Stream stream,
        LogReaderReadMode mode = LogReaderReadMode.All)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        // For async reading, we buffer the entire stream into memory asynchronously,
        // then run the synchronous parser on the memory stream.
        // This avoids duplicating complex parsing logic while getting true async IO.
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        if (ms.Length > MaxAllowedFileSize)
            throw new InvalidDataException(
                $"Stream size ({ms.Length} bytes) exceeds maximum allowed size ({MaxAllowedFileSize} bytes).");

        ms.Position = 0;

        // Check if compressed format (resets stream position)
        if (IsCompressedFormat(ms))
        {
            return CompressedLogReader.Read(ms);
        }

        return ParseFromStream(ms, mode);
    }

    private static TrajectoryLog ParseFromStream(Stream stream, LogReaderReadMode mode)
    {
        var utf8 = Encoding.UTF8;
        var log = new TrajectoryLog() { FilePath = string.Empty };
        var header = new Header();

        // We keep the stream open as it is managed by the caller (or the async wrapper)
        using (var br = new BinaryReader(stream, Encoding.UTF8, true))
        {
            // First SignatureSize bytes should contain 'VOSTL'
            var sigBytes = br.ReadBytes(SignatureSize);
            if (sigBytes.Length < SignatureSize)
                throw new EndOfStreamException(
                    $"File too short: expected at least {SignatureSize} bytes for signature.");

            var sig = utf8.GetString(sigBytes);

            if (!sig.StartsWith(ExpectedSignature))
                throw new InvalidDataException(
                    $"Invalid file signature '{sig.TrimEnd('\0')}'. Expected '{ExpectedSignature}'.");

            var versionBytes = br.ReadBytes(SignatureSize);
            if (versionBytes.Length < SignatureSize)
                throw new EndOfStreamException("File truncated while reading version.");

            var versionStr = utf8.GetString(versionBytes).TrimEnd('\0');
            if (!double.TryParse(versionStr, out var version))
                throw new InvalidDataException($"Invalid version format: '{versionStr}'.");
            header.Version = version;

            br.ReadInt32(); // header.HeaderSize - always 1024

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

            // MLC model selection based on header
            log.MlcModel = header.MlcModel == MLCModel.NDS120 ? new Millenium120MLC() : null;

            log.Header = header;
            log.MetaData = LogIOHelper.ReadMetaData(br.ReadBytes(MetaDataSize));

            // Skip the data if mode is HeaderAndMetaData
            if (mode == LogReaderReadMode.HeaderAndMetaData)
                return log;

            // Reserved bytes.
            // TrueBeam spec says offset 1024 - (64 + num_axis * 8) but it actually
            // includes metadata size (thanks pylinac source).
            // We just skip to HeaderSize.
            var reservedBytesToSkip = (HeaderSize - (64 + header.NumAxesSampled * 8)) - MetaDataSize;
            if (reservedBytesToSkip > 0)
                br.ReadBytes(reservedBytesToSkip);

            for (int i = 0; i < header.NumberOfSubBeams; i++)
            {
                log.SubBeams.Add(LogIOHelper.ReadSubBeam(br, log));
            }

            log.AxisData = new AxisData[header.NumAxesSampled];
            for (int i = 0; i < header.NumAxesSampled; i++)
            {
                var samplesForAxis = header.GetNumberOfSamples(i) * 2;
                log.AxisData[i] = new AxisData(header.NumberOfSnapshots, samplesForAxis);
            }

            for (int i = 0; i < header.NumberOfSnapshots; i++)
            {
                for (int j = 0; j < header.NumAxesSampled; j++)
                {
                    var axisData = log.AxisData[j];
                    var samplesForAxis = axisData.SamplesPerSnapshot;
                    var offset = i * samplesForAxis;

                    for (int k = 0; k < samplesForAxis; k++)
                    {
                        axisData.Data[offset + k] = br.ReadSingle();
                    }
                }
            }
        }

        return log;
    }

    /// <summary>
    /// Checks if a file is in compressed format by reading its signature.
    /// Detects both VOSTLC signature and GZip-wrapped compressed files.
    /// </summary>
    private static bool IsCompressedFormat(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        return IsCompressedFormat(fs);
    }

    /// <summary>
    /// Checks if a file is in compressed format by reading its signature asynchronously.
    /// </summary>
    private static async Task<bool> IsCompressedFormatAsync(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var buffer = new byte[SignatureSize];
        var bytesRead = await fs.ReadAsync(buffer, 0, SignatureSize);
        if (bytesRead < 2)
            return false;

        // Check for GZip magic bytes
        if (buffer[0] == GzipMagic1 && buffer[1] == GzipMagic2)
            return true;

        // Check for VOSTLC signature
        if (bytesRead >= CompressedSignature.Length)
        {
            var sig = Encoding.UTF8.GetString(buffer, 0, CompressedSignature.Length);
            return sig == CompressedSignature;
        }

        return false;
    }

    /// <summary>
    /// Checks if a stream contains compressed format data.
    /// Stream position is reset after checking.
    /// </summary>
    private static bool IsCompressedFormat(Stream stream)
    {
        var buffer = new byte[SignatureSize];
        var bytesRead = stream.Read(buffer, 0, SignatureSize);
        stream.Position = 0;

        if (bytesRead < 2)
            return false;

        // Check for GZip magic bytes
        if (buffer[0] == GzipMagic1 && buffer[1] == GzipMagic2)
            return true;

        // Check for VOSTLC signature
        if (bytesRead >= CompressedSignature.Length)
        {
            var sig = Encoding.UTF8.GetString(buffer, 0, CompressedSignature.Length);
            return sig == CompressedSignature;
        }

        return false;
    }
}