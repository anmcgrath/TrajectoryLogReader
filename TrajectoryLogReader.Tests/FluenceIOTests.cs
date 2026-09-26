using System.Globalization;
using Shouldly;
using TrajectoryLogReader.Extensions;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Tests;

public class FluenceIOTests
{
    // 3 cols x 2 rows, 2 mm x 5 mm pixels. Value = 10 * row + col, plus a value needing no group separator.
    private static GridF CreateGrid()
    {
        var grid = new GridF(new Rect(-3, -5, 6, 10), 3, 2);
        for (int row = 0; row < grid.Rows; row++)
        for (int col = 0; col < grid.Cols; col++)
            grid.SetData(col, row, 10 * row + col);
        grid.SetData(2, 1, 1234.5f);
        return grid;
    }

    [Test]
    public void SaveToTsv_Writes_Rows_Of_Columns()
    {
        var file = Path.GetTempFileName();
        try
        {
            CreateGrid().SaveToTsv(file);
            var lines = File.ReadAllLines(file);

            lines.Length.ShouldBe(2);
            lines[0].ShouldBe("0\t1\t2");
            lines[1].ShouldBe("10\t11\t1234.5");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Test]
    public void SaveToDat_Writes_Pixel_Centres_In_Mm_From_Top()
    {
        var file = Path.GetTempFileName();
        var culture = CultureInfo.CurrentCulture;
        try
        {
            // Decimal commas must not leak into the file
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CreateGrid().SaveToDat(file);
            var lines = File.ReadAllLines(file);

            lines.ShouldContain("XRESOLUTION\t2.000");
            lines.ShouldContain("YRESOLUTION\t5.000");
            lines.ShouldContain("XCOORDINATE\t-2.000");
            lines.ShouldContain("YCOORDINATE\t2.500");

            var xLine = lines.Single(l => l.StartsWith("0;"));
            xLine.ShouldBe("0;-2.000\t0.000\t2.000");

            var dataLines = lines.SkipWhile(l => !l.StartsWith("0;")).Skip(1).ToArray();
            dataLines.Length.ShouldBe(2);
            dataLines[0].ShouldBe("2.500\t10.000\t11.000\t1234.500");
            dataLines[1].ShouldBe("-2.500\t0.000\t1.000\t2.000");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            File.Delete(file);
        }
    }
}
