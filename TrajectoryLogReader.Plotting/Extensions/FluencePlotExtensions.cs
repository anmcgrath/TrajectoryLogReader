using ScottPlot;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Plotting.FluenceExtensions;

public static class FluencePlotExtensions
{
    /// <summary>
    /// Creates a heatmap plot of the fluence data.
    /// </summary>
    /// <param name="fluence">The fluence data to plot.</param>
    /// <param name="options">Optional settings for the heatmap display.</param>
    /// <returns>A ScottPlot Plot object.</returns>
    public static Plot CreateHeatmap(this FieldFluence fluence, FluenceHeatmapOptions? options = null)
        => fluence.Grid.CreateHeatmap(options);

    /// <summary>
    /// Creates a heatmap plot of the fluence data.
    /// </summary>
    /// <param name="grid">The grid data to plot.</param>
    /// <param name="options">Optional settings for the heatmap display.</param>
    /// <returns>A ScottPlot Plot object.</returns>
    public static Plot CreateHeatmap(this IGrid<float> grid, FluenceHeatmapOptions? options = null)
    {
        options ??= new FluenceHeatmapOptions();

        var coords = new Coordinates3d[grid.Rows, grid.Cols];
        for (int i = 0; i < grid.Rows; i++)
        {
            for (int j = 0; j < grid.Cols; j++)
            {
                coords[i, j] = new Coordinates3d(grid.GetX(i), grid.GetY(i), grid.GetData(j, i));
            }
        }

        var plot = new Plot();
        var hm = plot.Add.Heatmap(coords);
        hm.CellAlignment = Alignment.UpperRight;
        hm.Position = new CoordinateRect(new Coordinates(grid.Bounds.X, grid.Bounds.Y),
            new CoordinateSize(grid.Bounds.Width, grid.Bounds.Height));
        hm.FlipVertically = true;

        return plot;
    }
}

/// <summary>
/// Options for configuring fluence heatmap display.
/// </summary>
public record FluenceHeatmapOptions
{
}