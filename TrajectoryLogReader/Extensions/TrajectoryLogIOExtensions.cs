using System.Text;
using TrajectoryLogReader.IO;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Extensions;

public static class TrajectoryLogIOExtensions
{
    /// <param name="log"></param>
    extension(TrajectoryLog log)
    {
        /// <summary>
        /// Writes a TrajectoryLog to a file in Varian binary format (v5.0).
        /// </summary>
        /// <param name="fileName"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void SaveAs(string fileName)
        {
            if ((int)log.Header.Version != 5)
                throw new NotImplementedException($"Cannot save version {log.Header.Version}");

            TrajectoryLogWriterV5.Write(log, fileName);
        }

        public Task SaveAsAsync(string fileName)
        {
            if ((int)log.Header.Version != 5)
                throw new NotImplementedException($"Cannot save version {log.Header.Version}");

            return TrajectoryLogWriterV5.WriteAsync(log, fileName);
        }

        /// <summary>
        /// Save the trajectory log to a text file.
        /// </summary>
        /// <param name="fileName">The file to save to.</param>
        /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
        /// <param name="delimiter">The delimiter separating values in a line</param>
        /// <param name="scale">The scale the data is written as</param>
        /// <param name="axes">The axes to write. If empty, writes all axes.</param>
        public void SaveToText(string fileName,
            bool includeHeaders,
            char delimiter,
            AxisScale scale = AxisScale.Default,
            params Axis[] axes)
        {
            using var fs = File.OpenWrite(fileName);
            log.SaveToText(fs, includeHeaders, delimiter, scale, axes);
        }

        /// <summary>
        /// Save the trajectory log to a string builder
        /// </summary>
        /// <param name="sb">A string builder used for saving.</param>
        /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
        /// <param name="delimiter">The delimiter separating values in a line</param>
        ///     /// <param name="scale">The scale the data is written as</param>
        /// <param name="axes">The axes to write. If empty, writes all axes.</param>
        public void SaveToText(StringBuilder sb, bool includeHeaders, char delimiter,
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
        /// <param name="stream"></param>
        /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
        /// <param name="delimiter">The delimiter separating values in a line</param>
        ///     /// <param name="scale">The scale the data is written as</param>
        /// <param name="axes">The axes to write. If empty, writes all axes.</param>
        public void SaveToText(Stream stream, bool includeHeaders, char delimiter,
            AxisScale scale, Axis[] axes)
        {
            using var sw = new StreamWriter(stream);
            var sb = new StringBuilder();
            log.SaveToText(sb, includeHeaders, delimiter, scale, axes);
            sw.Write(sb);
        }

        /// <summary>
        /// Save the trajectory log to a .csv (comma seperated values) file.
        /// </summary>
        /// <param name="fileName">The CSV file name</param>
        /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
        /// <param name="scale">The scale the data is written as</param>
        /// <param name="axes">The axes to write. If empty, writes all axes.</param>
        public void SaveToCsv(string fileName, bool includeHeaders, AxisScale scale,
            Axis[] axes)
        {
            log.SaveToText(fileName, includeHeaders, ',', scale, axes);
        }

        /// <summary>
        /// Save the trajectory log to a tab seperated value file.
        /// </summary>
        /// <param name="fileName">The TSV file name</param>
        /// <param name="includeHeaders">If true, writes the axis headings as the first line.</param>
        /// <param name="scale">The scale the data is written as</param>
        /// <param name="axes">The axes to write. If empty, writes all axes.</param>
        public void SaveToTsv(string fileName, bool includeHeaders,
            AxisScale scale = AxisScale.Default,
            params Axis[] axes)
        {
            log.SaveToText(fileName, includeHeaders, '\t', scale, axes);
        }
    }
}