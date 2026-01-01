using System.Text;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Extensions;

public static class FluenceIO
{
    public static void SaveToTsv(this FieldFluence fluence, string fileName)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < fluence.Grid.SizeY; i++)
        {
            for (int j = 0; j < fluence.Grid.SizeX; j++)
            {
                sb.Append(fluence.Grid.Data[fluence.Grid.SizeY - 1 - i, j]);
                if (j != fluence.Grid.SizeX - 1)
                    sb.Append("\t");
            }

            if (i != fluence.Grid.SizeY - 1)
                sb.Append(Environment.NewLine);
        }

        File.WriteAllText(fileName, sb.ToString());
    }

    public static void SaveToDat(this FieldFluence fluence, string fileName)
    {
        var sb = new StringBuilder();
        // write header
        sb.AppendLine("PTW-Image File Format");
        sb.AppendLine("Version\t1.0");
        sb.AppendLine($"PIXELSPERLINE\t{fluence.Grid.SizeX}");
        sb.AppendLine($"LINESPERIMAGE\t{fluence.Grid.SizeY}");
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
            .Range(0, fluence.Grid.SizeX)
            .Select(x => $"{(10 * x * fluence.Grid.XRes + 10 * x0):N3}");

        sb.AppendLine($"0;" + string.Join("\t", xCoords));

        for (int i = 0; i < fluence.Grid.SizeY; i++)
        {
            sb.Append($"{(10 * y0 - 10 * fluence.Grid.YRes * i):N3}\t");
            for (int j = 0; j < fluence.Grid.SizeX; j++)
            {
                sb.Append($"{(fluence.Grid.Data[fluence.Grid.SizeY - 1 - i, j]):N3}");
                if (j != fluence.Grid.SizeX - 1)
                    sb.Append("\t");
            }

            if (i != fluence.Grid.SizeY - 1)
                sb.Append(Environment.NewLine);
        }

        File.WriteAllText(fileName, sb.ToString());
    }
}