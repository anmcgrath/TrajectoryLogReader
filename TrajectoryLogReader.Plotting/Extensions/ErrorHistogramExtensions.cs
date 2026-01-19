using ScottPlot;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Axes;
using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Plotting.Extensions;

public static class ErrorHistogramExtensions
{
    /// <summary>
    /// Creates an error histogram plot for the specified axis.
    /// </summary>
    /// <param name="axis">The axis accessor containing error data.</param>
    /// <param name="options">Optional histogram options.</param>
    /// <returns>A ScottPlot Plot object.</returns>
    public static Plot CreateErrorHistogram(this IAxisAccessor axis, ErrorHistogramOptions? options = null)
    {
        options ??= new ErrorHistogramOptions();
        var histogram = axis.ErrorHistogram(options.NumberOfBins);
        return CreateHistogramPlot(histogram, options);
    }

    /// <summary>
    /// Creates an error histogram plot for the MLC axis (all leaves).
    /// </summary>
    /// <param name="mlc">The MLC axis accessor.</param>
    /// <param name="options">Optional histogram options.</param>
    /// <returns>A ScottPlot Plot object.</returns>
    public static Plot CreateErrorHistogram(this MlcAxisAccessor mlc, ErrorHistogramOptions? options = null)
    {
        options ??= new ErrorHistogramOptions();
        var histogram = mlc.ErrorHistogram(options.NumberOfBins);
        return CreateHistogramPlot(histogram, options);
    }

    /// <summary>
    /// Creates an error histogram plot for a specific MLC bank.
    /// </summary>
    /// <param name="mlc">The MLC axis accessor.</param>
    /// <param name="bankIndex">The bank index (0 = Bank B, 1 = Bank A).</param>
    /// <param name="options">Optional histogram options.</param>
    /// <returns>A ScottPlot Plot object.</returns>
    public static Plot CreateErrorHistogramForBank(this MlcAxisAccessor mlc, int bankIndex, ErrorHistogramOptions? options = null)
    {
        options ??= new ErrorHistogramOptions();
        var bankLeaves = mlc.Leaves.Where(l => l.BankIndex == bankIndex);
        var allErrors = bankLeaves.SelectMany(l => l.ErrorValues).ToArray();
        var histogram = Histogram.FromData(allErrors, options.NumberOfBins);
        return CreateHistogramPlot(histogram, options);
    }

    /// <summary>
    /// Creates an error histogram plot for a specific MLC leaf.
    /// </summary>
    /// <param name="mlc">The MLC axis accessor.</param>
    /// <param name="bankIndex">The bank index (0 = Bank B, 1 = Bank A).</param>
    /// <param name="leafIndex">The leaf index.</param>
    /// <param name="options">Optional histogram options.</param>
    /// <returns>A ScottPlot Plot object, or null if the leaf was not found.</returns>
    public static Plot? CreateErrorHistogramForLeaf(this MlcAxisAccessor mlc, int bankIndex, int leafIndex, ErrorHistogramOptions? options = null)
    {
        var leaf = mlc.GetLeaf(bankIndex, leafIndex);
        if (leaf == null)
            return null;

        return leaf.CreateErrorHistogram(options);
    }

    /// <summary>
    /// Creates error histogram plots for all standard axes in the trajectory log.
    /// </summary>
    /// <param name="log">The trajectory log.</param>
    /// <param name="options">Optional histogram options.</param>
    /// <returns>A dictionary mapping axis names to their histogram plots.</returns>
    public static Dictionary<string, Plot> CreateAllAxisErrorHistograms(this TrajectoryLog log, ErrorHistogramOptions? options = null)
    {
        options ??= new ErrorHistogramOptions();
        var plots = new Dictionary<string, Plot>();
        var axes = log.Axes;

        // Gantry and Collimator
        plots["Gantry"] = axes.Gantry.CreateErrorHistogram(options with { Title = "Gantry Error Histogram" });
        plots["Collimator"] = axes.Collimator.CreateErrorHistogram(options with { Title = "Collimator Error Histogram" });

        // Jaws
        plots["X1"] = axes.X1.CreateErrorHistogram(options with { Title = "X1 Jaw Error Histogram" });
        plots["X2"] = axes.X2.CreateErrorHistogram(options with { Title = "X2 Jaw Error Histogram" });
        plots["Y1"] = axes.Y1.CreateErrorHistogram(options with { Title = "Y1 Jaw Error Histogram" });
        plots["Y2"] = axes.Y2.CreateErrorHistogram(options with { Title = "Y2 Jaw Error Histogram" });

        // Couch
        plots["CouchVrt"] = axes.CouchVrt.CreateErrorHistogram(options with { Title = "Couch Vertical Error Histogram" });
        plots["CouchLng"] = axes.CouchLng.CreateErrorHistogram(options with { Title = "Couch Longitudinal Error Histogram" });
        plots["CouchLat"] = axes.CouchLat.CreateErrorHistogram(options with { Title = "Couch Lateral Error Histogram" });
        plots["CouchRtn"] = axes.CouchRtn.CreateErrorHistogram(options with { Title = "Couch Rotation Error Histogram" });
        plots["CouchPitch"] = axes.CouchPitch.CreateErrorHistogram(options with { Title = "Couch Pitch Error Histogram" });
        plots["CouchRoll"] = axes.CouchRoll.CreateErrorHistogram(options with { Title = "Couch Roll Error Histogram" });

        // MLC
        plots["MLC"] = axes.Mlc.CreateErrorHistogram(options with { Title = "MLC Error Histogram (All Leaves)" });

        return plots;
    }

    /// <summary>
    /// Creates error histogram plots for MLC banks separately.
    /// </summary>
    /// <param name="log">The trajectory log.</param>
    /// <param name="options">Optional histogram options.</param>
    /// <returns>A dictionary with "BankA" and "BankB" plots.</returns>
    public static Dictionary<string, Plot> CreateMlcBankErrorHistograms(this TrajectoryLog log, ErrorHistogramOptions? options = null)
    {
        options ??= new ErrorHistogramOptions();
        var mlc = log.Axes.Mlc;

        return new Dictionary<string, Plot>
        {
            ["BankA"] = mlc.CreateErrorHistogramForBank(1, options with { Title = "MLC Bank A Error Histogram" }),
            ["BankB"] = mlc.CreateErrorHistogramForBank(0, options with { Title = "MLC Bank B Error Histogram" })
        };
    }

    private static Plot CreateHistogramPlot(Histogram histogram, ErrorHistogramOptions options)
    {
        var plot = new Plot();

        // Calculate bin width for bar positioning
        var binWidth = histogram.BinStarts.Length > 1
            ? histogram.BinStarts[1] - histogram.BinStarts[0]
            : 1f;

        // Create bar positions (center of each bin)
        var positions = histogram.BinStarts.Select(x => (double)(x + binWidth / 2)).ToArray();
        var values = histogram.Counts.Select(x => (double)x).ToArray();

        var bars = plot.Add.Bars(positions, values);
        bars.Color = options.BarColor;

        // Configure axes
        plot.Axes.Bottom.Label.Text = options.XAxisLabel;
        plot.Axes.Left.Label.Text = options.YAxisLabel;

        if (!string.IsNullOrEmpty(options.Title))
        {
            plot.Title(options.Title);
        }

        return plot;
    }
}

/// <summary>
/// Options for configuring error histogram plots.
/// </summary>
public record ErrorHistogramOptions
{
    /// <summary>
    /// Number of bins for the histogram. Default is 20.
    /// </summary>
    public int NumberOfBins { get; init; } = 20;

    /// <summary>
    /// Title for the plot. Default is empty.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Label for the X axis. Default is "Error".
    /// </summary>
    public string XAxisLabel { get; init; } = "Error";

    /// <summary>
    /// Label for the Y axis. Default is "Count".
    /// </summary>
    public string YAxisLabel { get; init; } = "Count";

    /// <summary>
    /// Color for the histogram bars.
    /// </summary>
    public Color BarColor { get; init; } = Colors.SteelBlue;
}
