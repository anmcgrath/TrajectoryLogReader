using System.IO.Compression;
using System.Text;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.IO;

/// <summary>
/// Writes a TrajectoryLog to a compressed binary format using delta encoding.
/// </summary>
/// <remarks>
/// Format overview:
/// - Header and metadata are stored similarly to V5 format for compatibility
/// - Axis data uses quantization with delta encoding
/// - Small axes (MLC, jaws, angles): 16-bit base + 8-bit deltas
/// - Large axes (couch positions, MU, ControlPoint): 32-bit base + 16-bit deltas
/// - Escape codes signal when a full absolute value follows
/// - Angular axes (Gantry, Collimator, CouchRtn) use normalized deltas for wraparound
/// </remarks>
public static class CompressedLogWriter
{
    /// <summary>
    /// File signature for compressed trajectory log files.
    /// </summary>
    private const string Signature = "VOSTLC";

    /// <summary>
    /// Version string for compressed format.
    /// </summary>
    private const string FormatVersion = "1.0";

    /// <summary>
    /// Size of the signature block in bytes.
    /// </summary>
    private const int SignatureSize = 16;


    /// <summary>
    /// Escape code for 8-bit delta streams indicating next value is absolute.
    /// </summary>
    private const sbyte EscapeCode8 = -128; // 0x80

    /// <summary>
    /// Escape code for 16-bit delta streams indicating next value is absolute.
    /// </summary>
    private const short EscapeCode16 = short.MinValue; // 0x8000

    /// <summary>
    /// Quantization scale for small position values (cm to 0.001 cm = 0.01 mm).
    /// Used for MLC leaves, jaws - range ±32 cm fits in 16-bit.
    /// </summary>
    private const float SmallPositionScale = 1000.0f;

    /// <summary>
    /// Quantization scale for large position values (cm to 0.01 cm = 0.1 mm).
    /// Used for couch positions - range ±21474 cm fits in 32-bit.
    /// </summary>
    private const float LargePositionScale = 100.0f;

    /// <summary>
    /// Quantization scale for angle values (degrees to 0.01 degrees).
    /// </summary>
    private const float AngleScale = 100.0f;

    /// <summary>
    /// Quantization scale for MU/ControlPoint values (to 0.001 units).
    /// </summary>
    private const float LargeValueScale = 1000.0f;

    /// <summary>
    /// Axes that represent 360-degree angular values and need wraparound handling.
    /// Note: CouchPitch and CouchRoll are NOT included - they're small tilt angles.
    /// </summary>
    private static readonly HashSet<Axis> FullRotationAxes = new()
    {
        Axis.GantryRtn,
        Axis.CollRtn,
        Axis.CouchRtn
    };

    /// <summary>
    /// Axes that require 32-bit storage due to large value ranges.
    /// Includes: couch positions, MU, ControlPoint, and full-rotation angles (0-360° = 36000 units at scale 100).
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
    /// Writes a TrajectoryLog to a file in compressed binary format.
    /// </summary>
    /// <param name="log">The trajectory log to write.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="useGzip">If true, applies GZip compression for additional space savings.</param>
    public static void Write(TrajectoryLog log, string filePath, bool useGzip = false)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        Write(log, fs, useGzip);
    }

    /// <summary>
    /// Writes a TrajectoryLog to a stream in compressed binary format.
    /// </summary>
    /// <param name="log">The trajectory log to write.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="useGzip">If true, applies GZip compression for additional space savings.</param>
    public static void Write(TrajectoryLog log, Stream stream, bool useGzip = false)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        LogIOHelper.ValidateLog(log);

        if (useGzip)
        {
            using var gzipStream = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
            WriteToStream(log, gzipStream);
        }
        else
        {
            WriteToStream(log, stream);
        }
    }

    private static void WriteToStream(TrajectoryLog log, Stream stream)
    {
        using var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        WriteSignatureAndVersion(bw);
        WriteHeader(bw, log.Header);
        LogIOHelper.WriteMetaData(bw, log.MetaData);
        WriteSubBeams(bw, log.SubBeams);
        WriteCompressedAxisData(bw, log);
    }

    private static void WriteSignatureAndVersion(BinaryWriter bw)
    {
        var sigBytes = new byte[SignatureSize];
        Encoding.UTF8.GetBytes(Signature, 0, Signature.Length, sigBytes, 0);
        bw.Write(sigBytes);

        var versionBytes = new byte[SignatureSize];
        Encoding.UTF8.GetBytes(FormatVersion, 0, FormatVersion.Length, versionBytes, 0);
        bw.Write(versionBytes);
    }

    private static void WriteHeader(BinaryWriter bw, Header header)
    {
        // Write original log version for round-trip compatibility
        bw.Write(header.Version);

        bw.Write(header.SamplingIntervalInMS);
        bw.Write(header.NumAxesSampled);

        for (int i = 0; i < header.NumAxesSampled; i++)
            bw.Write((int)header.AxesSampled[i]);

        for (int i = 0; i < header.NumAxesSampled; i++)
            bw.Write(header.SamplesPerAxis[i]);

        bw.Write((int)header.AxisScale);
        bw.Write(header.NumberOfSubBeams);
        bw.Write(header.IsTruncated ? 1 : 0);
        bw.Write(header.NumberOfSnapshots);
        bw.Write((int)header.MlcModel);
    }

    private static void WriteSubBeams(BinaryWriter bw, List<SubBeam> subBeams)
    {
        foreach (var subBeam in subBeams)
        {
            LogIOHelper.WriteSubBeam(bw, subBeam);
        }
    }

    private static void WriteCompressedAxisData(BinaryWriter bw, TrajectoryLog log)
    {
        var header = log.Header;

        for (int axisIndex = 0; axisIndex < header.NumAxesSampled; axisIndex++)
        {
            var axis = header.AxesSampled[axisIndex];
            var axisData = log.AxisData[axisIndex];
            var samplesPerSnapshot = axisData.SamplesPerSnapshot;
            var isFullRotation = FullRotationAxes.Contains(axis);
            var isLargeValue = LargeValueAxes.Contains(axis);
            var scale = GetScaleForAxis(axis);

            for (int sampleOffset = 0; sampleOffset < samplesPerSnapshot; sampleOffset++)
            {
                if (isLargeValue)
                {
                    WriteLargeValueStream(bw, axisData, sampleOffset, header.NumberOfSnapshots,
                        samplesPerSnapshot, isFullRotation, scale);
                }
                else
                {
                    WriteSmallValueStream(bw, axisData, sampleOffset, header.NumberOfSnapshots,
                        samplesPerSnapshot, scale);
                }
            }
        }
    }

    /// <summary>
    /// Writes a stream using 16-bit base values and 8-bit deltas.
    /// Used for MLC, jaws, small tilt angles (pitch/roll).
    /// </summary>
    private static void WriteSmallValueStream(
        BinaryWriter bw,
        AxisData axisData,
        int sampleOffset,
        int numSnapshots,
        int stride,
        float scale)
    {
        if (numSnapshots == 0) return;

        float firstValue = axisData.Data[sampleOffset];
        short firstQuantized = QuantizeToShort(firstValue, scale);
        bw.Write(firstQuantized);

        short previousQuantized = firstQuantized;

        for (int snapshot = 1; snapshot < numSnapshots; snapshot++)
        {
            int dataIndex = snapshot * stride + sampleOffset;
            float currentValue = axisData.Data[dataIndex];
            short currentQuantized = QuantizeToShort(currentValue, scale);

            int delta = currentQuantized - previousQuantized;

            // Check if delta fits in signed byte range [-127, 127]
            if (delta >= -127 && delta <= 127)
            {
                bw.Write((sbyte)delta);
            }
            else
            {
                bw.Write(EscapeCode8);
                bw.Write(currentQuantized);
            }

            previousQuantized = currentQuantized;
        }
    }

    /// <summary>
    /// Writes a stream using 32-bit base values and 16-bit deltas.
    /// Used for couch positions, MU, ControlPoint, and full-rotation angles.
    /// </summary>
    private static void WriteLargeValueStream(
        BinaryWriter bw,
        AxisData axisData,
        int sampleOffset,
        int numSnapshots,
        int stride,
        bool isFullRotation,
        float scale)
    {
        if (numSnapshots == 0) return;

        float firstValue = axisData.Data[sampleOffset];
        int firstQuantized = QuantizeToInt(firstValue, scale);
        bw.Write(firstQuantized);

        int previousQuantized = firstQuantized;

        for (int snapshot = 1; snapshot < numSnapshots; snapshot++)
        {
            int dataIndex = snapshot * stride + sampleOffset;
            float currentValue = axisData.Data[dataIndex];
            int currentQuantized = QuantizeToInt(currentValue, scale);

            long delta = (long)currentQuantized - previousQuantized;

            // For full rotation angles, normalize delta to handle 0/360 wraparound
            if (isFullRotation)
            {
                delta = NormalizeAngularDeltaLong(delta, scale);
            }

            // Check if delta fits in signed short range [-32767, 32767]
            // Reserve -32768 as escape code
            if (delta >= -32767 && delta <= 32767)
            {
                bw.Write((short)delta);
            }
            else
            {
                bw.Write(EscapeCode16);
                bw.Write(currentQuantized);
            }

            previousQuantized = currentQuantized;
        }
    }

    private static short QuantizeToShort(float value, float scale)
    {
        float scaled = value * scale;

        if (scaled > short.MaxValue || scaled < short.MinValue)
            throw new InvalidDataException($"Value {value} (scaled {scaled}) is out of range for 16-bit quantization.");

        return (short)Math.Round(scaled);
    }

    private static int QuantizeToInt(float value, float scale)
    {
        double scaled = (double)value * scale;

        if (scaled > int.MaxValue || scaled < int.MinValue)
            throw new InvalidDataException($"Value {value} (scaled {scaled}) is out of range for 32-bit quantization.");

        return (int)Math.Round(scaled);
    }

    private static long NormalizeAngularDeltaLong(long delta, float scale)
    {
        long halfCircle = (long)(180.0f * scale);
        long fullCircle = halfCircle * 2;

        while (delta > halfCircle)
            delta -= fullCircle;
        while (delta < -halfCircle)
            delta += fullCircle;

        return delta;
    }

    private static float GetScaleForAxis(Axis axis)
    {
        return axis switch
        {
            // Full rotation angles: 0.01° resolution
            Axis.GantryRtn or Axis.CollRtn or Axis.CouchRtn => AngleScale,

            // Small tilt angles: 0.01° resolution (not full rotation)
            Axis.CouchPitch or Axis.CouchRoll => AngleScale,

            // Large value axes: 0.01 unit resolution
            Axis.CouchVrt or Axis.CouchLng or Axis.CouchLat => LargePositionScale,
            Axis.MU or Axis.ControlPoint => LargeValueScale,

            // All other axes (MLC, jaws, etc.): 0.001 cm resolution
            _ => SmallPositionScale
        };
    }
}