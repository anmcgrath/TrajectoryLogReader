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
    private const string FormatVersion = "2.0";

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
    /// Default scale used when a stream has no deltas (constant values).
    /// </summary>
    private const float DefaultScale = 1000.0f;

    /// <summary>
    /// Minimum scale to avoid precision loss for very small deltas.
    /// </summary>
    private const float MinScale = 10.0f;

    /// <summary>
    /// Maximum scale to avoid overflow for very small ranges.
    /// </summary>
    private const float MaxScale = 100000.0f;

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
    public static void Write(TrajectoryLog log, string filePath, bool useGzip = true)
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
    public static void Write(TrajectoryLog log, Stream stream, bool useGzip = true)
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

        // First pass: calculate optimal scales for each stream
        var scales = new List<float>();
        for (int axisIndex = 0; axisIndex < header.NumAxesSampled; axisIndex++)
        {
            var axis = header.AxesSampled[axisIndex];
            var axisData = log.AxisData[axisIndex];
            var isLargeValue = LargeValueAxes.Contains(axis);
            var isFullRotation = FullRotationAxes.Contains(axis);

            for (int sampleOffset = 0; sampleOffset < axisData.SamplesPerSnapshot; sampleOffset++)
            {
                float scale = CalculateOptimalScale(
                    axisData, sampleOffset, header.NumberOfSnapshots,
                    axisData.SamplesPerSnapshot, isLargeValue, isFullRotation);
                scales.Add(scale);
            }
        }

        // Write scale table
        bw.Write(scales.Count);
        foreach (var scale in scales)
            bw.Write(scale);

        // Second pass: write compressed data using calculated scales
        int scaleIndex = 0;
        for (int axisIndex = 0; axisIndex < header.NumAxesSampled; axisIndex++)
        {
            var axis = header.AxesSampled[axisIndex];
            var axisData = log.AxisData[axisIndex];
            var samplesPerSnapshot = axisData.SamplesPerSnapshot;
            var isFullRotation = FullRotationAxes.Contains(axis);
            var isLargeValue = LargeValueAxes.Contains(axis);

            for (int sampleOffset = 0; sampleOffset < samplesPerSnapshot; sampleOffset++)
            {
                var scale = scales[scaleIndex++];

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
    /// Calculates the optimal quantization scale for a data stream based on actual delta values.
    /// Uses outlier detection (> 5 standard deviations) to optimize for typical movement patterns.
    /// Also ensures absolute values fit in the base storage type.
    /// </summary>
    private static float CalculateOptimalScale(
        AxisData axisData,
        int sampleOffset,
        int numSnapshots,
        int stride,
        bool isLargeValue,
        bool isFullRotation)
    {
        if (numSnapshots == 0)
            return DefaultScale;

        // Find the maximum absolute value to ensure it fits in base storage
        float maxAbsValue = 0;
        for (int snapshot = 0; snapshot < numSnapshots; snapshot++)
        {
            int dataIndex = snapshot * stride + sampleOffset;
            float absValue = Math.Abs(axisData.Data[dataIndex]);
            maxAbsValue = Math.Max(maxAbsValue, absValue);
        }

        // Calculate maximum scale that allows absolute values to fit in base storage
        // Leave 5% headroom for safety
        float baseStorageMax = isLargeValue ? int.MaxValue * 0.95f : short.MaxValue * 0.95f;
        float maxScaleForAbsValues = maxAbsValue > 1e-6f ? baseStorageMax / maxAbsValue : MaxScale;

        if (numSnapshots <= 1)
        {
            // Constant values - use max scale allowed by absolute values
            return Math.Max(MinScale, Math.Min(MaxScale, maxScaleForAbsValues));
        }

        // Collect deltas and calculate statistics in combined passes (no LINQ, no intermediate list)
        var (deltaCount, mean, variance, maxNormalDelta) = CalculateDeltaStatistics(
            axisData, sampleOffset, numSnapshots, stride, isFullRotation);

        if (deltaCount == 0)
        {
            // Constant values - use max scale allowed by absolute values
            return Math.Max(MinScale, Math.Min(MaxScale, maxScaleForAbsValues));
        }

        // If all deltas are negligible, use max scale allowed by absolute values
        if (maxNormalDelta < 1e-6f)
            return Math.Max(MinScale, Math.Min(MaxScale, maxScaleForAbsValues));

        // Calculate scale to fit normal deltas in available delta range
        // Leave 10% headroom for quantization rounding
        float deltaRange = isLargeValue ? 32767 * 0.9f : 127 * 0.9f;
        float scaleForDeltas = deltaRange / maxNormalDelta;

        // Use the minimum of delta-based scale and absolute-value-based scale
        float scale = Math.Min(scaleForDeltas, maxScaleForAbsValues);

        // Clamp to reasonable bounds
        return Math.Max(MinScale, Math.Min(MaxScale, scale));
    }

    /// <summary>
    /// Calculates delta statistics in two passes without allocations.
    /// Pass 1: Collect deltas and compute sum for mean.
    /// Pass 2: Compute variance and find max non-outlier delta.
    /// </summary>
    private static (int count, float mean, float variance, float maxNormalDelta) CalculateDeltaStatistics(
        AxisData axisData,
        int sampleOffset,
        int numSnapshots,
        int stride,
        bool isFullRotation)
    {
        int deltaCount = numSnapshots - 1;
        if (deltaCount <= 0)
            return (0, 0, 0, 0);

        // Pass 1: Calculate sum and collect deltas into a temporary span
        // We need to store deltas for pass 2, but use stackalloc for small counts
        float sum = 0;
        float previousValue = axisData.Data[sampleOffset];

        // For large snapshot counts, we'll do two separate iterations
        // For small counts, we could use stackalloc, but the threshold varies by platform
        // Instead, we'll iterate twice which is still faster than LINQ due to no allocations

        // First pass: compute mean
        for (int snapshot = 1; snapshot < numSnapshots; snapshot++)
        {
            int dataIndex = snapshot * stride + sampleOffset;
            float currentValue = axisData.Data[dataIndex];
            float delta = currentValue - previousValue;

            if (isFullRotation)
                delta = NormalizeAngularDeltaFloat(delta);

            sum += delta;
            previousValue = currentValue;
        }

        float mean = sum / deltaCount;

        // Second pass: compute variance
        float varianceSum = 0;
        previousValue = axisData.Data[sampleOffset];

        for (int snapshot = 1; snapshot < numSnapshots; snapshot++)
        {
            int dataIndex = snapshot * stride + sampleOffset;
            float currentValue = axisData.Data[dataIndex];
            float delta = currentValue - previousValue;

            if (isFullRotation)
                delta = NormalizeAngularDeltaFloat(delta);

            float diff = delta - mean;
            varianceSum += diff * diff;
            previousValue = currentValue;
        }

        float variance = varianceSum / deltaCount;
        float stdDev = (float)Math.Sqrt(variance);
        float outlierThreshold = Math.Abs(mean) + 5 * stdDev;

        // Third pass: find max non-outlier delta
        float maxNormalDelta = 0;
        previousValue = axisData.Data[sampleOffset];

        for (int snapshot = 1; snapshot < numSnapshots; snapshot++)
        {
            int dataIndex = snapshot * stride + sampleOffset;
            float currentValue = axisData.Data[dataIndex];
            float delta = currentValue - previousValue;

            if (isFullRotation)
                delta = NormalizeAngularDeltaFloat(delta);

            float absD = Math.Abs(delta);
            if (absD <= outlierThreshold && absD > maxNormalDelta)
                maxNormalDelta = absD;

            previousValue = currentValue;
        }

        return (deltaCount, mean, variance, maxNormalDelta);
    }

    /// <summary>
    /// Normalizes an angular delta to the range [-180, 180] degrees.
    /// </summary>
    private static float NormalizeAngularDeltaFloat(float delta)
    {
        while (delta > 180)
            delta -= 360;
        while (delta < -180)
            delta += 360;
        return delta;
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

}