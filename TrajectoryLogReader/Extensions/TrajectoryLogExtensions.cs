using System.Text;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Extensions;

public static class TrajectoryLogExtensions
{
    public static void Save(this TrajectoryLog log, string fileName, bool includeHeaders, char delimiter,
        params Axis[] axes)
    {
        using var fs = File.OpenWrite(fileName);
        Save(log, fs, includeHeaders, delimiter, axes);
    }

    public static void Save(this TrajectoryLog log, StringBuilder sb, bool includeHeaders, char delimiter,
        params Axis[] axes)
    {
        if (includeHeaders)
        {
            var headerLine = new StringBuilder();
            headerLine.Append($"time (ms){delimiter}");
            foreach (var axis in axes)
            {
                var axisIndex = log.Header.GetAxisIndex(axis);
                for (int i = 0; i < log.Header.GetNumberOfSamples(axisIndex); i++)
                {
                    headerLine.Append($"{axis}[{i}]");
                    if (i != log.Header.GetNumberOfSamples(axisIndex) - 1)
                        headerLine.Append(delimiter);
                }
            }

            sb.AppendLine(headerLine.ToString());
        }

        for (int s = 0; s < log.Header.NumberOfSnapshots; s++)
        {
            int t = s * log.Header.SamplingIntervalInMS;

            var line = new StringBuilder();
            line.Append($"{t}{delimiter}");
            foreach (var axis in axes)
            {
                var axisIndex = log.Header.GetAxisIndex(axis);
                for (int i = 0; i < log.Header.GetNumberOfSamples(axisIndex); i++)
                {
                    line.Append(log.GetAxisData(axis).RawData[s][i]);
                    if (i != log.Header.GetNumberOfSamples(axisIndex) - 1)
                        line.Append(delimiter);
                }
            }
        }
    }

    public static void Save(this TrajectoryLog log, Stream stream, bool includeHeaders, char delimiter,
        params Axis[] axes)
    {
        using var sw = new StreamWriter(stream);
        var sb = new StringBuilder();
        Save(log, sb, includeHeaders, delimiter, axes);
        sw.Write(sb);
    }
}