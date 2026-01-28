using System.Text;
using TrajectoryLogReader.IO;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Extensions;

/// <summary>
/// IO convenience methods for exporting trajectory logs into common analysis formats.
/// </summary>
public static class TrajectoryLogIOExtensions
{
    extension(TrajectoryLog log)
    {
        /// <summary>
        /// Writes the log back to the Varian binary trajectory format (v5.0 only).
        /// This is primarily useful for round-tripping edited or anonymized logs.
        /// </summary>
        /// <param name="fileName">The destination .bin file path.</param>
        /// <exception cref="NotImplementedException">
        /// Thrown when attempting to save a version other than 5.0.
        /// </exception>
        public void SaveAs(string fileName)
        {
            if ((int)log.Header.Version != 5)
                throw new NotImplementedException($"Cannot save version {log.Header.Version}");

            TrajectoryLogWriterV5.Write(log, fileName);
        }

        /// <summary>
        /// Asynchronously writes the log to the Varian binary trajectory format (v5.0 only).
        /// </summary>
        /// <param name="fileName">The destination .bin file path.</param>
        /// <returns>A task that completes when the file has been written.</returns>
        /// <exception cref="NotImplementedException">
        /// Thrown when attempting to save a version other than 5.0.
        /// </exception>
        public Task SaveAsAsync(string fileName)
        {
            if ((int)log.Header.Version != 5)
                throw new NotImplementedException($"Cannot save version {log.Header.Version}");

            return TrajectoryLogWriterV5.WriteAsync(log, fileName);
        }

        /// <summary>
        /// Exports the sampled axes to a delimited text file.
        /// </summary>
        /// <param name="fileName">The destination file path.</param>
        /// <param name="includeHeaders">If true, writes column headers on the first line.</param>
        /// <param name="delimiter">The delimiter separating values on each line.</param>
        /// <param name="scale">
        /// The coordinate system to export. Use <see cref="AxisScale.IEC61217"/> when you want
        /// standards-based sign conventions.
        /// </param>
        /// <param name="axes">
        /// The axes to export. When omitted, all sampled axes from the log header are used.
        /// </param>
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
        /// Exports the sampled axes to a <see cref="TextWriter"/> using a delimited layout.
        /// </summary>
        /// <param name="writer">The text writer destination.</param>
        /// <param name="includeHeaders">If true, writes column headers on the first line.</param>
        /// <param name="delimiter">The delimiter separating values on each line.</param>
        /// <param name="scale">The coordinate system to export.</param>
        /// <param name="axes">The axes to export. When empty, all sampled axes are used.</param>
        public void SaveToText(TextWriter writer, bool includeHeaders, char delimiter,
            AxisScale scale = AxisScale.Default,
            params Axis[] axes)
        {
            if (scale == AxisScale.Default)
                scale = log.Header.AxisScale;

            if (axes.Length == 0)
                axes = log.Header.AxesSampled;

            if (includeHeaders)
            {
                writer.Write($"time (ms){delimiter}");
                for (var axisIndex = 0; axisIndex < axes.Length; axisIndex++)
                {
                    var axis = axes[axisIndex];
                    var axisData = log.GetAxisData(axis);
                    var numSamples = axisData.SamplesPerSnapshot;
                    for (int i = 0; i < numSamples; i++)
                    {
                        writer.Write($"{axis}[{i}]");
                        if (i != numSamples - 1)
                            writer.Write(delimiter);
                    }

                    if (axisIndex != axes.Length - 1)
                        writer.Write(delimiter);
                }

                writer.WriteLine();
            }

            for (int s = 0; s < log.Header.NumberOfSnapshots; s++)
            {
                int t = s * log.Header.SamplingIntervalInMS;

                writer.Write($"{t}{delimiter}");
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
                        writer.Write(converted);
                        if (i != numSamples - 1)
                            writer.Write(delimiter);
                    }

                    if (axisIndex != axes.Length - 1)
                        writer.Write(delimiter);
                }

                writer.WriteLine();
            }
        }

        /// <summary>
        /// Exports the sampled axes to a <see cref="StringBuilder"/> using a delimited layout.
        /// This overload is useful when you want to capture the output in-memory (for example,
        /// to attach to a report or feed into another tool).
        /// </summary>
        /// <param name="sb">A string builder used for saving.</param>
        /// <param name="includeHeaders">If true, writes column headers on the first line.</param>
        /// <param name="delimiter">The delimiter separating values on each line.</param>
        /// <param name="scale">The coordinate system to export.</param>
        /// <param name="axes">The axes to export. When empty, all sampled axes are used.</param>
        public void SaveToText(StringBuilder sb, bool includeHeaders, char delimiter,
            AxisScale scale = AxisScale.Default,
            params Axis[] axes)
        {
            using var writer = new StringWriter(sb);
            log.SaveToText(writer, includeHeaders, delimiter, scale, axes);
        }

        /// <summary>
        /// Exports the sampled axes to a stream using a delimited layout.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="includeHeaders">If true, writes column headers on the first line.</param>
        /// <param name="delimiter">The delimiter separating values on each line.</param>
        /// <param name="scale">The coordinate system to export.</param>
        /// <param name="axes">The axes to export.</param>
        public void SaveToText(Stream stream, bool includeHeaders, char delimiter,
            AxisScale scale, params Axis[] axes)
        {
            using var sw = new StreamWriter(stream);
            log.SaveToText(sw, includeHeaders, delimiter, scale, axes);
        }

        /// <summary>
        /// Exports the sampled axes to a CSV (comma-separated values) file.
        /// </summary>
        /// <param name="fileName">The destination CSV file path.</param>
        /// <param name="includeHeaders">If true, writes column headers on the first line.</param>
        /// <param name="scale">The coordinate system to export.</param>
        /// <param name="axes">The axes to export.</param>
        public void SaveToCsv(string fileName, bool includeHeaders, AxisScale scale,
            params Axis[] axes)
        {
            log.SaveToText(fileName, includeHeaders, ',', scale, axes);
        }

        /// <summary>
        /// Exports the sampled axes to a TSV (tab-separated values) file.
        /// </summary>
        /// <param name="fileName">The destination TSV file path.</param>
        /// <param name="includeHeaders">If true, writes column headers on the first line.</param>
        /// <param name="scale">The coordinate system to export.</param>
        /// <param name="axes">The axes to export. When empty, all sampled axes are used.</param>
        public void SaveToTsv(string fileName, bool includeHeaders,
            AxisScale scale = AxisScale.Default,
            params Axis[] axes)
        {
            log.SaveToText(fileName, includeHeaders, '\t', scale, axes);
        }

        /// <summary>
        /// Writes the log to the library's compressed binary format (.cbin), which preserves
        /// the clinically relevant numeric content while reducing storage footprint.
        /// </summary>
        /// <param name="fileName">The destination .cbin file path.</param>
        public void SaveAsCompressed(string fileName)
        {
            CompressedLogWriter.Write(log, fileName, true);
        }
    }
}