using System.Text;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.IO;

/// <summary>
/// Writes a TrajectoryLog to the Varian binary trajectory log format (version 5.0).
/// </summary>
public static class TrajectoryLogWriterV5
{
    /// <summary>
    /// File signature for Varian trajectory log files.
    /// </summary>
    private const string Signature = "VOSTL";

    /// <summary>
    /// Version string for v5.0 format.
    /// </summary>
    private const string Version = "5.0";

    /// <summary>
    /// Size of the signature block in bytes.
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
    /// Writes a TrajectoryLog to a file in Varian binary format (v5.0).
    /// </summary>
    /// <param name="log">The trajectory log to write.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <exception cref="ArgumentNullException">Thrown when log or filePath is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the log data is incomplete or invalid.</exception>
    public static void Write(TrajectoryLog log, string filePath)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        Write(log, fs);
    }

    /// <summary>
    /// Writes a TrajectoryLog to a file in Varian binary format (v5.0) asynchronously.
    /// </summary>
    /// <param name="log">The trajectory log to write.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <exception cref="ArgumentNullException">Thrown when log or filePath is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the log data is incomplete or invalid.</exception>
    public static async Task WriteAsync(TrajectoryLog log, string filePath)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        using var ms = new MemoryStream();
        Write(log, ms);
        ms.Position = 0;

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await ms.CopyToAsync(fs);
    }

    /// <summary>
    /// Writes a TrajectoryLog to a stream in Varian binary format (v5.0).
    /// </summary>
    /// <param name="log">The trajectory log to write.</param>
    /// <param name="stream">The destination stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when log or stream is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the log data is incomplete or invalid.</exception>
    public static void Write(TrajectoryLog log, Stream stream)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        LogIOHelper.ValidateLog(log);
        ValidateLogV5(log);

        using var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        WriteSignatureAndVersion(bw);
        WriteHeader(bw, log.Header);
        LogIOHelper.WriteMetaData(bw, log.MetaData);
        WriteReservedBytes(bw, log.Header.NumAxesSampled);
        WriteSubBeams(bw, log.SubBeams);
        WriteAxisData(bw, log);
    }

    private static void ValidateLogV5(TrajectoryLog log)
    {
        // Additional V5-specific validation
        if (log.Header.NumAxesSampled != log.Header.AxesSampled.Length)
            throw new InvalidOperationException("Header.NumAxesSampled must match AxesSampled.Length.");
        if (log.Header.NumAxesSampled != log.Header.SamplesPerAxis.Length)
            throw new InvalidOperationException("Header.NumAxesSampled must match SamplesPerAxis.Length.");
        if (log.AxisData.Length != log.Header.NumAxesSampled)
            throw new InvalidOperationException("AxisData.Length must match Header.NumAxesSampled.");
    }

    private static void WriteSignatureAndVersion(BinaryWriter bw)
    {
        // Write signature (16 bytes, null-padded)
        var sigBytes = new byte[SignatureSize];
        Encoding.UTF8.GetBytes(Signature, 0, Signature.Length, sigBytes, 0);
        bw.Write(sigBytes);

        // Write version (16 bytes, null-padded)
        var versionBytes = new byte[SignatureSize];
        Encoding.UTF8.GetBytes(Version, 0, Version.Length, versionBytes, 0);
        bw.Write(versionBytes);
    }

    private static void WriteHeader(BinaryWriter bw, Header header)
    {
        bw.Write(HeaderSize);
        bw.Write(header.SamplingIntervalInMS);
        bw.Write(header.NumAxesSampled);

        // Write axis enum values
        for (int i = 0; i < header.NumAxesSampled; i++)
            bw.Write((int)header.AxesSampled[i]);

        // Write samples per axis
        for (int i = 0; i < header.NumAxesSampled; i++)
            bw.Write(header.SamplesPerAxis[i]);

        bw.Write((int)header.AxisScale);
        bw.Write(header.NumberOfSubBeams);
        bw.Write(header.IsTruncated ? 1 : 0);
        bw.Write(header.NumberOfSnapshots);
        bw.Write((int)header.MlcModel);
    }

    private static void WriteReservedBytes(BinaryWriter bw, int numAxesSampled)
    {
        // Calculate reserved bytes needed to reach header offset 1024
        // Formula from TrueBeam spec: reserved = 1024 - (64 + numAxes * 8) - metaDataSize
        var reservedSize = (HeaderSize - (64 + numAxesSampled * 8)) - MetaDataSize;
        if (reservedSize > 0)
            bw.Write(new byte[reservedSize]);
    }

    private static void WriteSubBeams(BinaryWriter bw, List<SubBeam> subBeams)
    {
        foreach (var subBeam in subBeams)
        {
            LogIOHelper.WriteSubBeam(bw, subBeam);
        }
    }

    private static void WriteAxisData(BinaryWriter bw, TrajectoryLog log)
    {
        var header = log.Header;

        // Write axis data: for each snapshot, for each axis, write all samples
        for (int snapshot = 0; snapshot < header.NumberOfSnapshots; snapshot++)
        {
            for (int axisIndex = 0; axisIndex < header.NumAxesSampled; axisIndex++)
            {
                var axisData = log.AxisData[axisIndex];
                var samplesPerSnapshot = axisData.SamplesPerSnapshot;
                var offset = snapshot * samplesPerSnapshot;

                for (int sample = 0; sample < samplesPerSnapshot; sample++)
                {
                    bw.Write(axisData.Data[offset + sample]);
                }
            }
        }
    }
}
