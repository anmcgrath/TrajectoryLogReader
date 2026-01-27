using System.Linq;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Tests;

public class GantryAndDoseRateTests
{
    private TrajectoryLog _log;
    private const int IntervalMs = 500; // 0.5s

    [SetUp]
    public void Setup()
    {
        _log = new TrajectoryLog();
        _log.Header = new Header();
        _log.Header.SamplingIntervalInMS = IntervalMs; 
        _log.Header.NumberOfSnapshots = 4;
        _log.Header.AxisScale = AxisScale.ModifiedIEC61217;
        
        // We will sample Gantry and MU
        _log.Header.AxesSampled = new[] { Axis.GantryRtn, Axis.MU };
        // Each has 2 values (Exp, Act)
        _log.Header.SamplesPerAxis = new[] { 2, 2 }; 

        _log.AxisData = new AxisData[2];

        // Gantry Data
        // Idx 0
        var gantryData = new AxisData(4, 2);
        gantryData.Data = new[] 
        {
            // Exp, Act
            0f, 0f,    // T0
            1f, 1f,    // T1 (Diff 1 deg) -> Speed = 1 / 0.5 = 2 deg/s
            3f, 3f,    // T2 (Diff 2 deg) -> Speed = 2 / 0.5 = 4 deg/s
            2f, 2f     // T3 (Diff -1 deg) -> Speed = -1 / 0.5 = -2 deg/s
        };
        _log.AxisData[0] = gantryData;

        // MU Data
        // Idx 1
        var muData = new AxisData(4, 2);
        muData.Data = new[]
        {
            // Exp, Act
            0f, 0f,     // T0
            1f, 1f,     // T1 (Diff 1 MU) -> Rate = 1 * (60/0.5) = 120 MU/min
            1.5f, 1.5f, // T2 (Diff 0.5 MU) -> Rate = 0.5 * 120 = 60 MU/min
            1.5f, 1.5f  // T3 (Diff 0) -> Rate = 0
        };
        _log.AxisData[1] = muData;
    }

    [Test]
    public void GantrySpeedColumnAccessCalculatesCorrectly()
    {
        _log.Axes.GantrySpeed.ActualValues.ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 2f, 4f, -2f
            });
        
        _log.Axes.GantrySpeed.ExpectedValues.ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 2f, 4f, -2f
            });
    }

    [Test]
    public void GantrySpeedRowAccessCalculatesCorrectly()
    {
        _log.Snapshots.Select(x => x.GantrySpeed.Actual)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 2f, 4f, -2f
            });

        _log.Snapshots.Select(x => x.GantrySpeed.Expected)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 2f, 4f, -2f
            });
    }

    [Test]
    public void DoseRateColumnAccessCalculatesCorrectly()
    {
        _log.Axes.DoseRate.ActualValues.ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 120f, 60f, 0f
            });

        _log.Axes.DoseRate.ExpectedValues.ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 120f, 60f, 0f
            });
    }

    [Test]
    public void GantryAccelerationCalculatesCorrectly()
    {
        // Gantry Speeds are [0, 2, 4, -2]
        // T1: (2 - 0) / 0.5 = 4 deg/s^2
        // T2: (4 - 2) / 0.5 = 4 deg/s^2
        // T3: (-2 - 4) / 0.5 = -12 deg/s^2
        _log.Axes.GantrySpeed.GetDelta(TimeSpan.FromSeconds(1)).ActualValues.ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 4f, 4f, -12f
            });
    }

    [Test]
    public void DoseRateRowAccessCalculatesCorrectly()
    {
        _log.Snapshots.Select(x => x.DoseRate.Actual)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 120f, 60f, 0f
            });

        _log.Snapshots.Select(x => x.DoseRate.Expected)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 120f, 60f, 0f
            });
    }

    [Test]
    public void FluentDeltaChainingMatchesGantrySpeed()
    {
        // snapshot.GantryRtn.GetDelta(1s) should equal snapshot.GantrySpeed
        _log.Snapshots.Select(x => x.GantryRtn.GetDelta(TimeSpan.FromSeconds(1)).Actual)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 2f, 4f, -2f
            });

        _log.Snapshots.Select(x => x.GantryRtn.GetDelta(TimeSpan.FromSeconds(1)).Expected)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 2f, 4f, -2f
            });
    }

    [Test]
    public void FluentDeltaChainingComputesAcceleration()
    {
        // snapshot.GantryRtn.GetDelta(1s).GetDelta(1s) computes acceleration
        // Gantry Speeds are [0, 2, 4, -2]
        // Accelerations:
        // T2: (4 - 2) / 0.5 = 4 deg/s^2
        // T3: (-2 - 4) / 0.5 = -12 deg/s^2
        _log.Snapshots.Select(x => x.GantryRtn.GetDelta(TimeSpan.FromSeconds(1)).GetDelta(TimeSpan.FromSeconds(1)).Actual)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 4f, 4f, -12f
            });
    }

    [Test]
    public void FluentDeltaChainingMatchesDoseRate()
    {
        // snapshot.MU.GetDelta(1 minute) should equal snapshot.DoseRate
        _log.Snapshots.Select(x => x.MU.GetDelta(TimeSpan.FromMinutes(1)).Actual)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0f, 120f, 60f, 0f
            });
    }

    [Test]
    public void RotationalAxisWrappingHandlesCorrectly()
    {
        // Test that gantry going 359° → 1° shows positive velocity (not -358°)
        var wrapLog = new TrajectoryLog();
        wrapLog.Header = new Header();
        wrapLog.Header.SamplingIntervalInMS = 1000; // 1 second for easy math
        wrapLog.Header.NumberOfSnapshots = 3;
        wrapLog.Header.AxisScale = AxisScale.ModifiedIEC61217;
        wrapLog.Header.AxesSampled = new[] { Axis.GantryRtn };
        wrapLog.Header.SamplesPerAxis = new[] { 2 };

        var gantryData = new AxisData(3, 2);
        gantryData.Data = new[]
        {
            // Exp, Act
            358f, 358f,  // T0
            360f, 360f,  // T1 (Diff 2 deg) -> Speed = 2 deg/s (but 360 normalizes)
            2f, 2f       // T2 (Diff 2 deg from 360→2, should wrap correctly) -> Speed = 2 deg/s
        };
        wrapLog.AxisData = new[] { gantryData };

        // The delta should be +2 deg/s, not -358 deg/s
        var velocities = wrapLog.Snapshots.Select(x => x.GantryRtn.GetDelta(TimeSpan.FromSeconds(1)).Actual).ToArray();
        velocities[0].ShouldBe(0f); // First snapshot has no previous
        velocities[1].ShouldBe(2f); // 360-358 = 2
        velocities[2].ShouldBe(2f); // 2-360 wraps to 2 (not -358)
    }
}
