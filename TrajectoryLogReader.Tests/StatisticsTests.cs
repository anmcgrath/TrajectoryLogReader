using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class StatisticsTests
{
    private TrajectoryLog _log;
    private const int NumSnapshots = 10;
    private const int SamplesPerAxis = 2; // Expected, Actual

    [SetUp]
    public void Setup()
    {
        _log = new TrajectoryLog();
        _log.Header = new Header
        {
            SamplingIntervalInMS = 20,
            NumberOfSnapshots = NumSnapshots,
            AxisScale = AxisScale.ModifiedIEC61217,
            AxesSampled = new[] { Axis.GantryRtn, Axis.Y1, Axis.MLC, Axis.MU }, // Gantry, Y1, MLC, MU
            MlcModel = MLCModel.NDS120, // 60 pairs
            // MLC Samples: (60 leaves * 2 banks) + 2 carriages = 122 samples.
            // SamplesPerSnapshot will be 122 * 2 = 244.
            SamplesPerAxis = new[] { 1, 1, 122, 1 }
        };
        _log.Header.NumAxesSampled = _log.Header.AxesSampled.Length;

        // Initialize AxisData
        _log.AxisData = new AxisData[_log.Header.NumAxesSampled];

        // 1. Gantry: Constant error of 1 degree
        // Expected: 0, 1, 2...
        // Actual: 1, 2, 3...
        var gantryData = new AxisData(NumSnapshots, 2);
        for (int i = 0; i < NumSnapshots; i++)
        {
            gantryData.Data[i * 2] = i; // Expected
            gantryData.Data[i * 2 + 1] = i + 1; // Actual (Error = 1)
        }

        _log.AxisData[0] = gantryData;

        // 2. Y1: Alternating error of +2 and -2
        // Expected: 10
        // Actual: 12 or 8
        var y1Data = new AxisData(NumSnapshots, 2);
        for (int i = 0; i < NumSnapshots; i++)
        {
            y1Data.Data[i * 2] = 10;
            y1Data.Data[i * 2 + 1] = (i % 2 == 0) ? 12 : 8; // Error is +2 or -2
        }

        _log.AxisData[1] = y1Data;

        // 3. MLC: Only Leaf 0 Bank 0 has error of 0.5, rest 0
        var mlcSamplesPerSnapshot = 122 * 2; // 244
        var mlcData = new AxisData(NumSnapshots, mlcSamplesPerSnapshot);
        for (int i = 0; i < NumSnapshots; i++)
        {
            // Set everything to 0
            Array.Clear(mlcData.Data, i * mlcSamplesPerSnapshot, mlcSamplesPerSnapshot);

            // Set Leaf 0, Bank 0 error to 0.5
            // Offset for Bank 0, Leaf 0: (0 * 60 * 2 + 0 * 2) + 4 = 4.
            // Index 4: Expected
            // Index 5: Actual
            int offset = i * mlcSamplesPerSnapshot + 4;
            mlcData.Data[offset] = 10.0f; // Expected
            mlcData.Data[offset + 1] = 10.5f; // Actual (Diff 0.5)
        }

        _log.AxisData[2] = mlcData;

        // 4. MU: Cumulative 2 MU per snapshot
        // Expected: 0, 2, 4...
        // Actual: 0, 2, 4... (No error)
        var muData = new AxisData(NumSnapshots, 2);
        for (int i = 0; i < NumSnapshots; i++)
        {
            muData.Data[i * 2] = i * 2; // Expected
            muData.Data[i * 2 + 1] = i * 2; // Actual
        }

        _log.AxisData[3] = muData;
    }

    [Test]
    public void RootMeanSquareError_ScalarAxis_ReturnsCorrectRMS()
    {
        // Gantry: Error is always 1. RMS = Sqrt(Sum(1^2)/N) = Sqrt(N/N) = 1.
        _log.Statistics.RootMeanSquareError(Axis.GantryRtn).ShouldBe(1.0f, 0.001f);

        // Y1: Error is +2 or -2. Sq error is 4. RMS = Sqrt(Sum(4)/N) = Sqrt(4N/N) = 2.
        _log.Statistics.RootMeanSquareError(Axis.Y1).ShouldBe(2.0f, 0.001f);
    }

    [Test]
    public void MaxError_ScalarAxis_ReturnsMaxAbsoluteError()
    {
        // Gantry: Max error is 1.
        _log.Statistics.MaxError(Axis.GantryRtn).ShouldBe(1.0f, 0.001f);

        Math.Abs(_log.Statistics.MaxError(Axis.Y1)).ShouldBe(2.0f, 0.001f);
    }

    [Test]
    public void RootMeanSquareError_MLC_ReturnsCorrectRMS()
    {
        var expectedRms = Math.Sqrt(2.5 / (10 * 120));
        _log.Statistics.RootMeanSquareError(Axis.MLC).ShouldBe((float)expectedRms, 0.0001f);
    }

    [Test]
    public void RootMeanSquareError_RotationalAxis_HandlesWrapAround()
    {
        // Gantry with wrap around
        // Exp: 359, Act: 1 -> Diff should be 2, not -358.
        var gantryData = _log.GetAxisData(Axis.GantryRtn);
        // Reset data
        for (int i = 0; i < NumSnapshots; i++)
        {
            gantryData.Data[i * 2] = 359;
            gantryData.Data[i * 2 + 1] = 1;
        }

        // Error is 2. RMS should be 2.
        _log.Statistics.RootMeanSquareError(Axis.GantryRtn).ShouldBe(2.0f, 0.001f);
    }

    [Test]
    public void IndividualMlcLeaf_ReturnsCorrectStatistics()
    {
        // Setup: Leaf 0, Bank 0 has constant error of 0.5 (set in Setup)
        // Leaf 1, Bank 0 has 0 error.

        // RMS for Leaf 0, Bank 0 should be 0.5
        _log.Statistics.RootMeanSquareError(0, 0).ShouldBe(0.5f, 0.001f);

        // Max Error for Leaf 0, Bank 0 should be 0.5
        _log.Statistics.MaxError(0, 0).ShouldBe(0.5f, 0.001f);

        // RMS for Leaf 1, Bank 0 should be 0
        _log.Statistics.RootMeanSquareError(0, 1).ShouldBe(0f, 0.001f);

        // Max Error for Leaf 1, Bank 0 should be 0.5 (Wait, test said 0.5 for leaf 0, 0 for leaf 1)
        _log.Statistics.MaxError(0, 1).ShouldBe(0f, 0.001f);
    }

    [Test]
    public void BinByAxis_BinsValuesCorrectly()
    {
        var result = _log.Statistics.BinByAxis(
            s => s.Y1.Expected,
            Axis.GantryRtn,
            RecordType.ExpectedPosition,
            0, 10, 2);
        result.Values[0].ShouldBe(20);
        result.Values[1].ShouldBe(20);
    }

    [Test]
    public void GetMuPerGantryAngle_ReturnsCorrectMuDistribution()
    {
        var result = _log.Statistics.GetMuPerGantryAngle(2, RecordType.ExpectedPosition);
        result.Values[0].ShouldBe(2.0f, 0.001f);
        result.Values[1].ShouldBe(4.0f, 0.001f);

        TrajectoryLog log;
    }
}