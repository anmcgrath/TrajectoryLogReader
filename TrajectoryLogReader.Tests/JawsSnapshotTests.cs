using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Tests;

public class JawsSnapshotTests
{
    private TrajectoryLog _log;
    private const int IntervalMs = 1000; // 1 second

    [SetUp]
    public void Setup()
    {
        _log = new TrajectoryLog();
        _log.Header = new Header();
        _log.Header.SamplingIntervalInMS = IntervalMs;
        _log.Header.NumberOfSnapshots = 3;
        _log.Header.AxisScale = AxisScale.ModifiedIEC61217;

        // Sample X1, X2, Y1, Y2
        _log.Header.AxesSampled = new[] { Axis.X1, Axis.X2, Axis.Y1, Axis.Y2 };
        _log.Header.SamplesPerAxis = new[] { 2, 2, 2, 2 };

        _log.AxisData = new AxisData[4];

        // X1 Data (Index 0)
        var x1Data = new AxisData(3, 2);
        x1Data.Data = new[]
        {
            // Exp, Act
            5.0f, 5.0f,  // T0
            5.1f, 5.1f,  // T1
            5.2f, 5.2f   // T2
        };
        _log.AxisData[0] = x1Data;

        // X2 Data (Index 1)
        var x2Data = new AxisData(3, 2);
        x2Data.Data = new[]
        {
            // Exp, Act
            5.0f, 5.0f,  // T0
            5.1f, 5.1f,  // T1
            5.2f, 5.2f   // T2
        };
        _log.AxisData[1] = x2Data;

        // Y1 Data (Index 2)
        var y1Data = new AxisData(3, 2);
        y1Data.Data = new[]
        {
            // Exp, Act
            10.0f, 10.0f, // T0
            10.1f, 10.1f, // T1
            10.2f, 10.2f  // T2
        };
        _log.AxisData[2] = y1Data;

        // Y2 Data (Index 3)
        var y2Data = new AxisData(3, 2);
        y2Data.Data = new[]
        {
            // Exp, Act
            2.0f, 2.0f,   // T0
            2.1f, 2.1f,   // T1
            2.2f, 2.2f    // T2
        };
        _log.AxisData[3] = y2Data;
    }

    [Test]
    public void JawsX_Sums_X1_And_X2()
    {
        // JawsX = X1 + X2
        // T0: 5 + 5 = 10
        // T1: 5.1 + 5.1 = 10.2
        // T2: 5.2 + 5.2 = 10.4
        
        var jawsX = _log.Snapshots.Select(s => s.JawsX.Actual).ToArray();
        
        jawsX[0].ShouldBe(10.0f, 0.001f);
        jawsX[1].ShouldBe(10.2f, 0.001f);
        jawsX[2].ShouldBe(10.4f, 0.001f);
    }

    [Test]
    public void JawsY_Sums_Y1_And_Y2()
    {
        // JawsY = Y1 + Y2
        // T0: 10 + 2 = 12
        // T1: 10.1 + 2.1 = 12.2
        // T2: 10.2 + 2.2 = 12.4
        
        var jawsY = _log.Snapshots.Select(s => s.JawsY.Actual).ToArray();
        
        jawsY[0].ShouldBe(12.0f, 0.001f);
        jawsY[1].ShouldBe(12.2f, 0.001f);
        jawsY[2].ShouldBe(12.4f, 0.001f);
    }

    [Test]
    public void JawsX_Delta_Calculates_Speed()
    {
        // JawsX Speeds (cm/s)
        // Delta = Current - Previous
        // Interval = 1s
        // T0: 0
        // T1: 10.2 - 10.0 = 0.2
        // T2: 10.4 - 10.2 = 0.2
        
        var speeds = _log.Snapshots
            .Select(s => s.JawsX.GetDelta(TimeSpan.FromSeconds(1)).Actual)
            .ToArray();
        
        speeds[0].ShouldBe(0.0f, 0.001f);
        speeds[1].ShouldBe(0.2f, 0.001f);
        speeds[2].ShouldBe(0.2f, 0.001f);
    }
}