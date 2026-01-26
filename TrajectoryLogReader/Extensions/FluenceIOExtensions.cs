using System.Text;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Extensions;

/// <summary>
/// Extension methods for saving fluence data to files.
/// </summary>
public static class FluenceIOExtensions
{
    /// <summary>
    /// Saves the fluence grid to a Tab-Separated Values (TSV) file.
    /// </summary>
    /// <param name="fluence">The field fluence.</param>
    /// <param name="fileName">The output file name.</param>
    public static void SaveToTsv(this FieldFluence fluence, string fileName)
    {
        var sb = new StringBuilder();
        var cols = fluence.Grid.Cols;
        for (int i = 0; i < fluence.Grid.Rows; i++)
        {
            int rowOffset = (fluence.Grid.Rows - 1 - i) * cols;
            for (int j = 0; j < cols; j++)
            {
                sb.Append(fluence.Grid.Data[rowOffset + j]);
                if (j != cols - 1)
                    sb.Append("\t");
            }

            if (i != fluence.Grid.Rows - 1)
                sb.Append(Environment.NewLine);
        }

        File.WriteAllText(fileName, sb.ToString());
    }

    /// <summary>
    /// Saves the fluence grid to a PTW-Image File Format (.dat) file.
    /// </summary>
    /// <param name="fluence">The field fluence.</param>
    /// <param name="fileName">The output file name.</param>
    public static void SaveToDat(this FieldFluence fluence, string fileName)
    {
        var sb = new StringBuilder();
        // write header
        sb.AppendLine("PTW-Image File Format");
        sb.AppendLine("Version\t1.0");
        sb.AppendLine($"PIXELSPERLINE\t{fluence.Grid.Cols}");
        sb.AppendLine($"LINESPERIMAGE\t{fluence.Grid.Rows}");
        sb.AppendLine($"XRESOLUTION\t{(fluence.Grid.XRes * 10):N3}");
        sb.AppendLine($"YRESOLUTION\t{(fluence.Grid.YRes * 10):N3}");
        var x0 = -fluence.Grid.Width / 2;
        var y0 = fluence.Grid.Height / 2;
        sb.AppendLine($"XCOORDINATE\t{(x0 * 10):N3}");
        sb.AppendLine($"YCOORDINATE\t{(y0 * 10):N3}");
        sb.AppendLine("OFFSET\t\t0.00");
        sb.AppendLine("UNIT\t\tGy");
        sb.AppendLine("SOFTWARE\tLOGFILEANALYSER");
        sb.AppendLine("NORMALIZATION\t100.000");

        // x coords
        var xCoords = Enumerable
            .Range(0, fluence.Grid.Cols)
            .Select(x => $"{(10 * x * fluence.Grid.XRes + 10 * x0):N3}");

        sb.AppendLine($"0;" + string.Join("\t", xCoords));

        var cols = fluence.Grid.Cols;
        for (int i = 0; i < fluence.Grid.Rows; i++)
        {
            sb.Append($"{(10 * y0 - 10 * fluence.Grid.YRes * i):N3}\t");
            int rowOffset = (fluence.Grid.Rows - 1 - i) * cols;
            for (int j = 0; j < cols; j++)
            {
                sb.Append($"{(fluence.Grid.Data[rowOffset + j]):N3}");
                if (j != cols - 1)
                    sb.Append("\t");
            }

            if (i != fluence.Grid.Rows - 1)
                sb.Append(Environment.NewLine);
        }

        File.WriteAllText(fileName, sb.ToString());
    }
}