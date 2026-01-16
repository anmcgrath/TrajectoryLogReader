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
    private const int MetaDataSize = 745;

    /// <summary>
    /// Maximum allowed file size (500 MB)
    /// </summary>
    private const long MaxAllowedFileSize = 500 * 1024 * 1024;

    /// <summary>
    /// Expected file signature prefix for Varian trajectory log files.
    /// </summary>
    private const string ExpectedSignature = "VOSTL";

    /// <summary>
    /// Reads a binary (*.bin) trajectory log file (Varian) asynchronously.
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
            throw new InvalidDataException($"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({MaxAllowedFileSize} bytes).");

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var log = await ReadBinaryAsync(fs, mode);
        log.FilePath = filePath;
        return log;
    }

    /// <summary>
    /// Reads a binary (*.bin) trajectory log file (Varian).
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
            throw new InvalidDataException($"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({MaxAllowedFileSize} bytes).");

        using var fs = File.OpenRead(filePath);
        var log = ReadBinary(fs, mode);
        log.FilePath = filePath;
        return log;
    }

    /// <summary>
    /// Reads a binary (*.bin) trajectory log file (Varian) from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="mode">If set to HeaderAndMetaData, the reader does not read log data, only the header and metadata.</param>
    /// <returns>The parsed trajectory log.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file format is invalid.</exception>
    public static TrajectoryLog ReadBinary(Stream stream, LogReaderReadMode mode = LogReaderReadMode.All)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        return ParseFromStream(stream, mode);
    }

    /// <summary>
    /// Reads a binary (*.bin) trajectory log file (Varian) from a stream asynchronously.
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
            throw new InvalidDataException($"Stream size ({ms.Length} bytes) exceeds maximum allowed size ({MaxAllowedFileSize} bytes).");

        ms.Position = 0;
        return ParseFromStream(ms, mode);
    }

    private static TrajectoryLog ParseFromStream(Stream stream, LogReaderReadMode mode)
    {
        var utf8 = Encoding.UTF8;
        var log = new TrajectoryLog() { FilePath = null };
        var header = new Header();

        // We keep the stream open as it is managed by the caller (or the async wrapper)
        using (var br = new BinaryReader(stream, Encoding.UTF8, true))
        {
            // First SignatureSize bytes should contain 'VOSTL'
            var sigBytes = br.ReadBytes(SignatureSize);
            if (sigBytes.Length < SignatureSize)
                throw new EndOfStreamException($"File too short: expected at least {SignatureSize} bytes for signature.");

            var sig = utf8.GetString(sigBytes);

            if (!sig.StartsWith(ExpectedSignature))
                throw new InvalidDataException($"Invalid file signature '{sig.TrimEnd('\0')}'. Expected '{ExpectedSignature}'.");

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
            log.MetaData = ReadMetaData(br.ReadBytes(MetaDataSize), utf8);

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
                var subBeam = new SubBeam(log);
                subBeam.ControlPoint = br.ReadInt32();
                subBeam.MU = br.ReadSingle();
                subBeam.RadTime = br.ReadSingle();
                subBeam.SequenceNumber = br.ReadInt32();
                subBeam.Name = utf8.GetString(br.ReadBytes(512)).Trim().Trim('\t', '\0');
                br.ReadBytes(32);
                log.SubBeams.Add(subBeam);
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

    private static MetaData ReadMetaData(byte[] bytes, Encoding strEncoding)
    {
        var metaData = new MetaData();
        var metaDataStr = strEncoding.GetString(bytes);
        var lines = metaDataStr.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var lineSplit = line.Split(new[] { ':' }, 2);
            if (lineSplit.Length < 2)
                continue; // Skip malformed lines

            var type = lineSplit[0];
            var val = lineSplit[1];

            switch (type)
            {
                case "Patient ID":
                    metaData.PatientId = val.Trim().Trim('\t', '\0');
                    break;
                case "Plan Name":
                    metaData.PlanName = val.Trim().Trim('\t', '\0');
                    break;
                case "Plan UID":
                    metaData.PlanUID = val.Trim().Trim('\t', '\0');
                    break;
                case "Original MU":
                    if (double.TryParse(val.Trim(), out var muPlanned))
                        metaData.MUPlanned = muPlanned;
                    break;
                case "Remaining MU":
                    if (double.TryParse(val.Trim(), out var muRemaining))
                        metaData.MURemaining = muRemaining;
                    break;
                case "Energy":
                    metaData.Energy = val.Trim().Trim('\t', '\0');
                    break;
                case "BeamName":
                    metaData.BeamName = val.Trim().Trim('\t', '\0');
                    break;
            }
        }

        return metaData;
    }
}