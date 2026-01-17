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
    private const int MetaDataSize = 745;

    /// <summary>
    /// Size of the sub-beam name field in bytes.
    /// </summary>
    private const int SubBeamNameSize = 512;

    /// <summary>
    /// Size of the sub-beam reserved field in bytes.
    /// </summary>
    private const int SubBeamReservedSize = 32;

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

        ValidateLog(log);

        using var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        WriteSignatureAndVersion(bw);
        WriteHeader(bw, log.Header);
        WriteMetaData(bw, log.MetaData);
        WriteReservedBytes(bw, log.Header.NumAxesSampled);
        WriteSubBeams(bw, log.SubBeams);
        WriteAxisData(bw, log);
    }

    private static void ValidateLog(TrajectoryLog log)
    {
        if (log.Header == null)
            throw new InvalidOperationException("TrajectoryLog.Header cannot be null.");
        if (log.MetaData == null)
            throw new InvalidOperationException("TrajectoryLog.MetaData cannot be null.");
        if (log.AxisData == null)
            throw new InvalidOperationException("TrajectoryLog.AxisData cannot be null.");
        if (log.Header.AxesSampled == null)
            throw new InvalidOperationException("Header.AxesSampled cannot be null.");
        if (log.Header.SamplesPerAxis == null)
            throw new InvalidOperationException("Header.SamplesPerAxis cannot be null.");
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

    private static void WriteMetaData(BinaryWriter bw, MetaData metaData)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(metaData.PatientId))
            sb.Append($"Patient ID:{metaData.PatientId}\r\n");
        if (!string.IsNullOrEmpty(metaData.PlanName))
            sb.Append($"Plan Name:{metaData.PlanName}\r\n");
        if (!string.IsNullOrEmpty(metaData.PlanUID))
            sb.Append($"Plan UID:{metaData.PlanUID}\r\n");
        if (metaData.MUPlanned > 0)
            sb.Append($"Original MU:{metaData.MUPlanned}\r\n");
        if (metaData.MURemaining > 0)
            sb.Append($"Remaining MU:{metaData.MURemaining}\r\n");
        if (!string.IsNullOrEmpty(metaData.Energy))
            sb.Append($"Energy:{metaData.Energy}\r\n");
        if (!string.IsNullOrEmpty(metaData.BeamName))
            sb.Append($"BeamName:{metaData.BeamName}\r\n");

        var metaBytes = new byte[MetaDataSize];
        var metaStr = sb.ToString();
        if (metaStr.Length > 0)
            Encoding.UTF8.GetBytes(metaStr, 0, Math.Min(metaStr.Length, MetaDataSize), metaBytes, 0);

        bw.Write(metaBytes);
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
            bw.Write(subBeam.ControlPoint);
            bw.Write(subBeam.MU);
            bw.Write(subBeam.RadTime);
            bw.Write(subBeam.SequenceNumber);

            // Write name (512 bytes, null-padded)
            var nameBytes = new byte[SubBeamNameSize];
            if (!string.IsNullOrEmpty(subBeam.Name))
            {
                var nameStr = subBeam.Name;
                Encoding.UTF8.GetBytes(nameStr, 0, Math.Min(nameStr.Length, SubBeamNameSize), nameBytes, 0);
            }
            bw.Write(nameBytes);

            // Write reserved bytes (32 bytes)
            bw.Write(new byte[SubBeamReservedSize]);
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
