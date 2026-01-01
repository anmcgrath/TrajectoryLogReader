using System.Text;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Util;

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
    /// <param name="scale">The scale the data is written as</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void Save(this TrajectoryLog log,
        string fileName,
        bool includeHeaders,
        char delimiter,
        AxisScale scale = AxisScale.Default,
        params Axis[] axes)
    {
        using var fs = File.OpenWrite(fileName);
        Save(log, fs, includeHeaders, delimiter, scale, axes);
    }

    /// <summary>
    /// Save the trajectory log to a string builder
    /// </summary>
    /// <param name="log"></param>
    /// <param name="sb">A string builder used for saving.</param>
    /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
    /// <param name="delimiter">The delimiter separating values in a line</param>
    ///     /// <param name="scale">The scale the data is written as</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void Save(this TrajectoryLog log, StringBuilder sb, bool includeHeaders, char delimiter,
        AxisScale scale = AxisScale.Default,
        params Axis[] axes)
    {
        if (scale == AxisScale.Default)
            scale = log.Header.AxisScale;

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
                var numSamples = axisData.SamplesPerSnapshot;
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
                var numSamples = axisData.SamplesPerSnapshot;
                var offset = s * numSamples;
                
                for (int i = 0; i < numSamples; i++)
                {
                    var val = axisData.Data[offset + i];
                    var converted = Scale.Convert(log.Header.AxisScale, scale, axis, val);
                    line.Append(converted);
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
    ///     /// <param name="scale">The scale the data is written as</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void Save(this TrajectoryLog log, Stream stream, bool includeHeaders, char delimiter,
        AxisScale scale, Axis[] axes)
    {
        using var sw = new StreamWriter(stream);
        var sb = new StringBuilder();
        Save(log, sb, includeHeaders, delimiter, scale, axes);
        sw.Write(sb);
    }

    /// <summary>
    /// Save the trajectory log to a .csv (comma seperated values) file.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="fileName">The CSV file name</param>
    /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
    /// <param name="scale">The scale the data is written as</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void SaveToCsv(this TrajectoryLog log, string fileName, bool includeHeaders, AxisScale scale,
        Axis[] axes)
    {
        Save(log, fileName, includeHeaders, ',', scale, axes);
    }

    /// <summary>
    /// Save the trajectory log to a tab seperated value file.
    /// </summary>
    /// <param name="log"></param>
    /// <param name="fileName">The TSV file name</param>
    /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
    /// <param name="scale">The scale the data is written as</param>
    /// <param name="axes">The axes to write. If empty, writes all axes.</param>
    public static void SaveToTsv(this TrajectoryLog log, string fileName, bool includeHeaders,
        AxisScale scale = AxisScale.Default,
        params Axis[] axes)
    {
        Save(log, fileName, includeHeaders, '\t', scale, axes);
    }
}
