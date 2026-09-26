using TrajectoryLogReader.Complexity;
using TrajectoryLogReader.DICOM.FluenceAdapters;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.DICOM.Plan;

public class BeamModel
{
    public string BeamName { get; set; }
    public int BeamNumber { get; set; }
    public PrimaryDosimeterUnit PrimaryDosimeterUnit { get; set; }
    public int NumberOfControlPoints => ControlPoints.Count;
    public float Energy { get; set; }
    public RadiationType RadiationType { get; set; }
    public BeamType BeamType { get; set; }
    public string BeamDescription { get; set; }
    public string TreatmentMachineName { get; set; }
    public FluenceMode PrimaryFluenceMode { get; set; }
    public List<ApplicatorModel> Applicators { get; set; } = new();
    public List<BlockModel> Blocks { get; set; } = new();
    public List<WedgeModel> Wedges { get; set; } = new();
    public List<ControlPointData> ControlPoints { get; set; } = new();
    public float MU { get; set; }
    public IMLCModel? Mlc { get; set; }

    /// <summary>
    /// The cumulative meterset weight at the end of the beam. Control point cumulative meterset weights
    /// are relative to this value, which is not necessarily 1. If not set, the last control point's
    /// cumulative meterset weight is used.
    /// </summary>
    public float? FinalCumulativeMetersetWeight { get; set; }

    /// <summary>
    /// Returns the MU delivered up to the cumulative meterset weight <paramref name="cumulativeMetersetWeight"/>.
    /// </summary>
    public float GetCumulativeMu(float cumulativeMetersetWeight)
    {
        var finalWeight = FinalCumulativeMetersetWeight ?? ControlPoints.LastOrDefault()?.CumulativeMetersetWeight ?? 0;
        if (finalWeight <= 0)
            throw new InvalidOperationException($"Final cumulative meterset weight must be > 0: {finalWeight}");

        return cumulativeMetersetWeight / finalWeight * MU;
    }

    /// <summary>
    /// Calculates the Average Leaf Pair Opening (ALPO) for this sub-beam.
    /// ALPO measures the average gap between opposing MLC leaves for leaf pairs within the jaw opening.
    /// </summary>
    /// <param name="options">Calculation options. If null, default options are used.</param>
    /// <returns>The average leaf pair opening in centimeters.</returns>
    public double CalculateAverageLeafPairOpening(AverageLeafPairOpeningOptions? options)
    {
        var alpoOptions = options ?? new AverageLeafPairOpeningOptions();
        var adapter = new BeamCollectionAdapter(this, 1);
        return AverageLeafPairOpeningCalculator.Calculate(adapter, alpoOptions);
    }
}