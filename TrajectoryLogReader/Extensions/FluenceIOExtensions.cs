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
    public static void SaveToTsv(this FieldFluence fluence, string fileName) => fluence.Grid.SaveToTsv(fileName);

    /// <summary>
    /// Saves the grid to a Tab-Separated Values (TSV) file.
    /// </summary>
    /// <param name="grid">The grid.</param>
    /// <param name="fileName">The output file name.</param>
    public static void SaveToTsv(this IGrid<float> grid, string fileName)
    {
        var sb = new StringBuilder();
        var cols = grid.Cols;
        for (int i = 0; i < grid.Rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                sb.Append(grid.GetData(i, j));
                if (j != cols - 1)
                    sb.Append("\t");
            }

            if (i != grid.Rows - 1)
                sb.Append(Environment.NewLine);
        }

        File.WriteAllText(fileName, sb.ToString());
    }

    /// <summary>
    /// Saves the fluence grid to a PTW-Image File Format (.dat) file.
    /// </summary>
    /// <param name="grid">The field fluence.</param>
    /// <param name="fileName">The output file name.</param>
    public static void SaveToDat(this IGrid<float> grid, string fileName)
    {
        var sb = new StringBuilder();
        // write header
        sb.AppendLine("PTW-Image File Format");
        sb.AppendLine("Version\t1.0");
        sb.AppendLine($"PIXELSPERLINE\t{grid.Cols}");
        sb.AppendLine($"LINESPERIMAGE\t{grid.Rows}");
        sb.AppendLine($"XRESOLUTION\t{(grid.XRes * 10):N3}");
        sb.AppendLine($"YRESOLUTION\t{(grid.YRes * 10):N3}");
        var x0 = grid.Bounds.X;
        var y0 = grid.Bounds.Y;
        sb.AppendLine($"XCOORDINATE\t{(x0 * 10):N3}");
        sb.AppendLine($"YCOORDINATE\t{(y0 * 10):N3}");
        sb.AppendLine("OFFSET\t\t0.00");
        sb.AppendLine("UNIT\t\tGy");
        sb.AppendLine("SOFTWARE\tLOGFILEANALYSER");
        sb.AppendLine("NORMALIZATION\t100.000");

        // x coords
        var xCoords = Enumerable
            .Range(0, grid.Cols)
            .Select(x => $"{(10 * x * grid.XRes + 10 * x0):N3}");

        sb.AppendLine($"0;" + string.Join("\t", xCoords));

        var cols = grid.Cols;
        for (int i = 0; i < grid.Rows; i++)
        {
            sb.Append($"{(10 * y0 - 10 * grid.YRes * i):N3}\t");
            for (int j = 0; j < cols; j++)
            {
                sb.Append($"{(grid.GetData(i, j)):N3}");
                if (j != cols - 1)
                    sb.Append("\t");
            }

            if (i != grid.Rows - 1)
                sb.Append(Environment.NewLine);
        }

        File.WriteAllText(fileName, sb.ToString());
    }

    /// <summary>
    /// Saves the fluence grid to a PTW-Image File Format (.dat) file.
    /// </summary>
    /// <param name="fluence">The field fluence.</param>
    /// <param name="fileName">The output file name.</param>
    public static void SaveToDat(this FieldFluence fluence, string fileName) => fluence.Grid.SaveToDat(fileName);
}