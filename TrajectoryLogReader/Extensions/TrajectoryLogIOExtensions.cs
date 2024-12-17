using System.Text;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Extensions;

public static class TrajectoryLogIOExtensions
{
    /// <summary>
    /// Save the trajectory log to a file.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="fileName">The file to save to.</param>
    /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
    /// <param name="delimiter">The delimiter separating values in a line</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void Save(this TrajectoryLog log, string fileName, bool includeHeaders, char delimiter,
        params Axis[] axes)
    {
        using var fs = File.OpenWrite(fileName);
        Save(log, fs, includeHeaders, delimiter, axes);
    }

    /// <summary>
    /// Save the trajectory log to a string builder
    /// </summary>
    /// <param name="log"></param>
    /// <param name="sb">A string builder used for saving.</param>
    /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
    /// <param name="delimiter">The delimiter separating values in a line</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void Save(this TrajectoryLog log, StringBuilder sb, bool includeHeaders, char delimiter,
        params Axis[] axes)
    {
        if (axes.Length == 0)
            axes = log.Header.AxesSampled;

        if (includeHeaders)
        {
            var headerLine = new StringBuilder();
            headerLine.Append($"time (ms){delimiter}");
            for (var axisIndex = 0; axisIndex < axes.Length; axisIndex++)
            {
                var axis = axes[axisIndex];
                var axisData = log.GetAxisData(axis);
                var numSamples = axisData.RawData[0].Length;
                for (int i = 0; i < numSamples; i++)
                {
                    headerLine.Append($"{axis}[{i}]");
                    if (i != numSamples - 1)
                        headerLine.Append(delimiter);
                }

                if (axisIndex != axes.Length - 1)
                    headerLine.Append(delimiter);
            }

            sb.AppendLine(headerLine.ToString());
        }

        for (int s = 0; s < log.Header.NumberOfSnapshots; s++)
        {
            int t = s * log.Header.SamplingIntervalInMS;

            var line = new StringBuilder();
            line.Append($"{t}{delimiter}");
            for (var axisIndex = 0; axisIndex < axes.Length; axisIndex++)
            {
                var axis = axes[axisIndex];
                var axisData = log.GetAxisData(axis);
                var numSamples = axisData.RawData[0].Length;
                for (int i = 0; i < numSamples; i++)
                {
                    line.Append(axisData.RawData[s][i]);
                    if (i != numSamples - 1)
                        line.Append(delimiter);
                }

                if (axisIndex != axes.Length - 1)
                    line.Append(delimiter);
            }

            sb.AppendLine(line.ToString());
        }
    }

    /// <summary>
    /// Save the trajectory log to a stream
    /// </summary>
    /// <param name="log"></param>
    /// <param name="stream"></param>
    /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
    /// <param name="delimiter">The delimiter separating values in a line</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void Save(this TrajectoryLog log, Stream stream, bool includeHeaders, char delimiter,
        params Axis[] axes)
    {
        using var sw = new StreamWriter(stream);
        var sb = new StringBuilder();
        Save(log, sb, includeHeaders, delimiter, axes);
        sw.Write(sb);
    }

    /// <summary>
    /// Save the trajectory log to a .csv (comma seperated values) file.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="fileName">The CSV file name</param>
    /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void SaveToCsv(this TrajectoryLog log, string fileName, bool includeHeaders, params Axis[] axes)
    {
        Save(log, fileName, includeHeaders, ',', axes);
    }

    /// <summary>
    /// Save the trajectory log to a tab seperated value file.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="fileName">The TSV file name</param>
    /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void SaveToTsv(this TrajectoryLog log, string fileName, bool includeHeaders, params Axis[] axes)
    {
        Save(log, fileName, includeHeaders, '\t', axes);
    }
}