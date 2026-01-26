using System.Text;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.IO;

/// <summary>
/// Shared helper methods for reading and writing trajectory log components.
/// Used by both standard and compressed log readers/writers.
/// </summary>
internal static class LogIOHelper
{
    /// <summary>
    /// Size of the metadata block in bytes.
    /// </summary>
    public const int MetaDataSize = 745;

    /// <summary>
    /// Size of the sub-beam name field in bytes.
    /// </summary>
    public const int SubBeamNameSize = 512;

    /// <summary>
    /// Size of the sub-beam reserved field in bytes.
    /// </summary>
    public const int SubBeamReservedSize = 32;

    /// <summary>
    /// Parses metadata from a byte array.
    /// </summary>
    public static MetaData ReadMetaData(byte[] bytes)
    {
        var metaData = new MetaData();
        var metaDataStr = Encoding.UTF8.GetString(bytes);
        var lines = metaDataStr.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var lineSplit = line.Split(new[] { ':' }, 2);
            if (lineSplit.Length < 2)
                continue;

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

    /// <summary>
    /// Writes metadata to a binary writer.
    /// </summary>
    public static void WriteMetaData(BinaryWriter bw, MetaData metaData)
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

    /// <summary>
    /// Reads a single sub-beam from a binary reader.
    /// </summary>
    public static SubBeam ReadSubBeam(BinaryReader br, TrajectoryLog log)
    {
        var subBeam = new SubBeam(log)
        {
            ControlPoint = br.ReadInt32(),
            MU = br.ReadSingle(),
            RadTime = br.ReadSingle(),
            SequenceNumber = br.ReadInt32(),
            Name = Encoding.UTF8.GetString(br.ReadBytes(SubBeamNameSize)).Trim().Trim('\t', '\0')
        };
        br.ReadBytes(SubBeamReservedSize); // Reserved
        return subBeam;
    }

    /// <summary>
    /// Writes a single sub-beam to a binary writer.
    /// </summary>
    public static void WriteSubBeam(BinaryWriter bw, SubBeam subBeam)
    {
        bw.Write(subBeam.ControlPoint);
        bw.Write(subBeam.MU);
        bw.Write(subBeam.RadTime);
        bw.Write(subBeam.SequenceNumber);

        var nameBytes = new byte[SubBeamNameSize];
        if (!string.IsNullOrEmpty(subBeam.Name))
        {
            var nameStr = subBeam.Name;
            Encoding.UTF8.GetBytes(nameStr, 0, Math.Min(nameStr.Length, SubBeamNameSize), nameBytes, 0);
        }
        bw.Write(nameBytes);

        bw.Write(new byte[SubBeamReservedSize]);
    }

    /// <summary>
    /// Validates a trajectory log before writing.
    /// </summary>
    public static void ValidateLog(TrajectoryLog log)
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
    }
}
