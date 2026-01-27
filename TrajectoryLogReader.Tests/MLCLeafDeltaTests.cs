using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class MLCLeafDeltaTests
{
    private TrajectoryLog _log;
    private const int NumSnapshots = 4;
    private const int SamplingInterval = 20; // 20ms = 0.02s
    private const float Tolerance = 0.001f;

    [SetUp]
    public void Setup()
    {
        _log = new TrajectoryLog();
        _log.Header = new Header
        {
            SamplingIntervalInMS = SamplingInterval,
            NumberOfSnapshots = NumSnapshots,
            AxisScale = AxisScale.MachineScale,
            AxesSampled = new[] { Axis.MLC },
            SamplesPerAxis = new[] { 122 },
            MlcModel = MLCModel.NDS120
        };
        _log.Header.NumAxesSampled = 1;
        _log.AxisData = new AxisData[1];

        var mlcData = new AxisData(NumSnapshots, 122 * 2);

        // Leaf 0, Bank 0 (index 4) - linear motion
        // t0: 0.0 cm
        // t1: 0.1 cm (delta = 0.1, speed = 0.1/0.02 = 5 cm/s)
        // t2: 0.3 cm (delta = 0.2, speed = 0.2/0.02 = 10 cm/s)
        // t3: 0.4 cm (delta = 0.1, speed = 0.1/0.02 = 5 cm/s)
        SetLeafPosition(mlcData, 0, 0, 0, 0.0f, 0.0f);
        SetLeafPosition(mlcData, 1, 0, 0, 0.1f, 0.1f);
        SetLeafPosition(mlcData, 2, 0, 0, 0.3f, 0.3f);
        SetLeafPosition(mlcData, 3, 0, 0, 0.4f, 0.4f);

        // Leaf 1, Bank 0 (index 6) - with error
        // Expected: 0.0, 0.1, 0.2, 0.3
        // Actual:   0.0, 0.12, 0.22, 0.32
        SetLeafPosition(mlcData, 0, 0, 1, 0.0f, 0.0f);
        SetLeafPosition(mlcData, 1, 0, 1, 0.1f, 0.12f);
        SetLeafPosition(mlcData, 2, 0, 1, 0.2f, 0.22f);
        SetLeafPosition(mlcData, 3, 0, 1, 0.3f, 0.32f);

        _log.AxisData[0] = mlcData;
    }

    private void SetLeafPosition(AxisData data, int snapshot, int bank, int leaf, float expected, float actual)
    {
        // MLC data layout: first 4 values are carriages, then leaves
        // Each bank has 60 leaves (for NDS120), each with Expected/Actual pair
        var numLeaves = 60;
        var baseOffset = snapshot * 122 * 2;
        var leafOffset = 4 + (bank * numLeaves * 2) + (leaf * 2);
        data.Data[baseOffset + leafOffset] = expected;
        data.Data[baseOffset + leafOffset + 1] = actual;
    }

    [Test]
    public void MLCLeafDelta_CalculatesCorrectly()
    {
        // Get delta for leaf 0, bank 0
        var deltas = _log.Snapshots.Select(s => s.MLC.GetLeaf(0, 0).GetDelta().Actual).ToArray();

        deltas[0].ShouldBe(0f, Tolerance);    // First snapshot has no previous
        deltas[1].ShouldBe(0.1f, Tolerance);  // 0.1 - 0.0
        deltas[2].ShouldBe(0.2f, Tolerance);  // 0.3 - 0.1
        deltas[3].ShouldBe(0.1f, Tolerance);  // 0.4 - 0.3
    }

    [Test]
    public void MLCLeafDelta_WithTimeSpan_CalculatesSpeed()
    {
        // Speed = delta / time = delta / 0.02s
        // With TimeSpan of 1 second, multiplier is 1000ms / 20ms = 50
        var speeds = _log.Snapshots
            .Select(s => s.MLC.GetLeaf(0, 0).GetDelta(TimeSpan.FromSeconds(1)).Actual)
            .ToArray();

        speeds[0].ShouldBe(0f, Tolerance);     // First snapshot has no previous
        speeds[1].ShouldBe(5f, Tolerance);     // 0.1 * 50 = 5 cm/s
        speeds[2].ShouldBe(10f, Tolerance);    // 0.2 * 50 = 10 cm/s
        speeds[3].ShouldBe(5f, Tolerance);     // 0.1 * 50 = 5 cm/s
    }

    [Test]
    public void MLCLeafDelta_ChainingComputesAcceleration()
    {
        // Speeds: [0, 5, 10, 5] cm/s
        // Accelerations:
        // T0: 0 (no previous delta)
        // T1: (5 - 0) / 0.02 = 250 cm/s^2
        // T2: (10 - 5) / 0.02 = 250 cm/s^2
        // T3: (5 - 10) / 0.02 = -250 cm/s^2
        var accelerations = _log.Snapshots
            .Select(s => s.MLC.GetLeaf(0, 0)
                .GetDelta(TimeSpan.FromSeconds(1))
                .GetDelta(TimeSpan.FromSeconds(1)).Actual)
            .ToArray();

        accelerations[0].ShouldBe(0f, Tolerance);      // No previous delta
        accelerations[1].ShouldBe(250f, Tolerance);    // (5 - 0) / 0.02
        accelerations[2].ShouldBe(250f, Tolerance);    // (10 - 5) / 0.02
        accelerations[3].ShouldBe(-250f, Tolerance);   // (5 - 10) / 0.02
    }

    [Test]
    public void MLCLeafDelta_TracksExpectedAndActualSeparately()
    {
        // Leaf 1 has different expected/actual
        // Expected deltas: 0, 0.1, 0.1, 0.1
        // Actual deltas: 0, 0.12, 0.1, 0.1
        var expectedDeltas = _log.Snapshots
            .Select(s => s.MLC.GetLeaf(0, 1).GetDelta().Expected)
            .ToArray();
        var actualDeltas = _log.Snapshots
            .Select(s => s.MLC.GetLeaf(0, 1).GetDelta().Actual)
            .ToArray();

        expectedDeltas[0].ShouldBe(0f, Tolerance);
        expectedDeltas[1].ShouldBe(0.1f, Tolerance);
        expectedDeltas[2].ShouldBe(0.1f, Tolerance);
        expectedDeltas[3].ShouldBe(0.1f, Tolerance);

        actualDeltas[0].ShouldBe(0f, Tolerance);
        actualDeltas[1].ShouldBe(0.12f, Tolerance);
        actualDeltas[2].ShouldBe(0.1f, Tolerance);
        actualDeltas[3].ShouldBe(0.1f, Tolerance);
    }

    [Test]
    public void MLCLeafDelta_ErrorReflectsDeltaDifference()
    {
        // Delta errors for leaf 1
        // Expected deltas: 0, 0.1, 0.1, 0.1
        // Actual deltas: 0, 0.12, 0.1, 0.1
        // Errors: 0, 0.02, 0, 0
        var errors = _log.Snapshots
            .Select(s => s.MLC.GetLeaf(0, 1).GetDelta().Error)
            .ToArray();

        errors[0].ShouldBe(0f, Tolerance);
        errors[1].ShouldBe(0.02f, Tolerance);
        errors[2].ShouldBe(0f, Tolerance);
        errors[3].ShouldBe(0f, Tolerance);
    }

    [Test]
    public void MLCLeafDelta_MatchesColumnBasedVelocity()
    {
        // Verify row-based GetDelta(1s) matches column-based GetVelocity()
        // Both should return velocity in cm/s
        var columnVelocityCmPerSec = _log.Axes.Mlc.GetLeaf(Bank.A, 0).GetVelocity().ActualValues.ToArray();

        // Row-based with TimeSpan of 1 second gives velocity in cm/s
        var rowVelocityCmPerSec = _log.Snapshots
            .Select(s => s.MLC.GetLeaf(0, 0).GetDelta(TimeSpan.FromSeconds(1)).Actual)
            .ToArray();

        for (int i = 0; i < columnVelocityCmPerSec.Length; i++)
        {
            rowVelocityCmPerSec[i].ShouldBe(columnVelocityCmPerSec[i], Tolerance,
                $"Velocity mismatch at snapshot {i}");
        }
    }
}
