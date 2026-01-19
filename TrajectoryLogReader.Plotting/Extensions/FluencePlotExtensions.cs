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
    {
        options ??= new FluenceHeatmapOptions();

        var grid = fluence.Grid;
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

        // Overlay jaw outlines if requested
        /*if (options.ShowJawOutlines && fluence.JawOutlines.Count > 0)
        {
            foreach (var outline in fluence.JawOutlines)
            {
                // Convert Point[] to Coordinates[] for ScottPlot polygon
                // Close the polygon by repeating the first point
                var polygonCoords = new Coordinates[outline.Length + 1];
                for (int i = 0; i < outline.Length; i++)
                {
                    polygonCoords[i] = new Coordinates(outline[i].X, outline[i].Y);
                }

                polygonCoords[outline.Length] = polygonCoords[0]; // Close the polygon

                var poly = plot.Add.Polygon(polygonCoords);
                poly.LineColor = options.JawOutlineColor;
                poly.LineWidth = options.JawOutlineWidth;
                poly.FillColor = Colors.Transparent;
            }
        }*/

        return plot;
    }
}

/// <summary>
/// Options for configuring fluence heatmap display.
/// </summary>
public record FluenceHeatmapOptions
{
    /// <summary>
    /// Whether to show the jaw outlines overlaid on the heatmap. Default is true.
    /// </summary>
    public bool ShowJawOutlines { get; init; } = false;

    /// <summary>
    /// Color for the jaw outline. Default is white.
    /// </summary>
    public Color JawOutlineColor { get; init; } = Colors.White;

    /// <summary>
    /// Line width for the jaw outline. Default is 2.
    /// </summary>
    public float JawOutlineWidth { get; init; } = 2f;
}