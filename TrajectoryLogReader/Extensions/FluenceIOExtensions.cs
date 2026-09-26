using System.Globalization;
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
                sb.Append(grid.GetData(j, i).ToString(CultureInfo.InvariantCulture));
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
    /// Coordinates are pixel centres in mm, with lines written from the top (+Y) down.
    /// </summary>
    /// <param name="grid">The field fluence.</param>
    /// <param name="fileName">The output file name.</param>
    public static void SaveToDat(this IGrid<float> grid, string fileName)
    {
        static string Format(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        // write header
        sb.AppendLine("PTW-Image File Format");
        sb.AppendLine("Version\t1.0");
        sb.AppendLine($"PIXELSPERLINE\t{grid.Cols}");
        sb.AppendLine($"LINESPERIMAGE\t{grid.Rows}");
        sb.AppendLine($"XRESOLUTION\t{Format(grid.XRes)}");
        sb.AppendLine($"YRESOLUTION\t{Format(grid.YRes)}");
        sb.AppendLine($"XCOORDINATE\t{Format(grid.GetX(0))}");
        sb.AppendLine($"YCOORDINATE\t{Format(grid.GetY(grid.Rows - 1))}");
        sb.AppendLine("OFFSET\t\t0.00");
        sb.AppendLine("UNIT\t\tGy");
        sb.AppendLine("SOFTWARE\tLOGFILEANALYSER");
        sb.AppendLine("NORMALIZATION\t100.000");

        // x coords
        var xCoords = Enumerable
            .Range(0, grid.Cols)
            .Select(x => Format(grid.GetX(x)));

        sb.AppendLine($"0;" + string.Join("\t", xCoords));

        var cols = grid.Cols;
        for (int i = 0; i < grid.Rows; i++)
        {
            // Grid row 0 is at the bottom (-Y), lines are written from the top
            var row = grid.Rows - 1 - i;
            sb.Append($"{Format(grid.GetY(row))}\t");
            for (int j = 0; j < cols; j++)
            {
                sb.Append(Format(grid.GetData(j, row)));
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