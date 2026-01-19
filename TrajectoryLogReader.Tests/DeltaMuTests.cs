using Shouldly;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Tests;

public class DeltaMuTests
{
    private TrajectoryLog _log;

    [SetUp]
    public void Setup()
    {
        _log = new TrajectoryLog();
        _log.Header = new Header();
        _log.Header.SamplesPerAxis = new[] { 2 };
        _log.Header.NumberOfSnapshots = 4;
        _log.Header.AxisScale = AxisScale.ModifiedIEC61217;
        _log.Header.AxesSampled = new[] { Axis.MU }; // Gantry, Y1, MLC, MU
        _log.AxisData = new AxisData[1];
        _log.AxisData[_log.Header.GetAxisIndex(Axis.MU)] = new AxisData(10, 2)
        {
            Data = new[]
            {
                0, 0,
                1, 1,
                2, 2,
                3, 3.5f
            }
        };
    }

    [Test]
    public void DeltaMuColumnAccessCalculatesCorrectly()
    {
        _log.Axes.DeltaMu.ActualValues.ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0, 1, 1, 1.5f
            });
        _log.Axes.DeltaMu.ExpectedValues.ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0, 1, 1, 1.0f
            });
        _log.Axes.DeltaMu.ErrorValues.ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0, 0, 0, 0.5f
            });
    }

    [Test]
    public void DeltaMuRowAccessCalculatesCorrectly()
    {
        _log.Snapshots.Select(x => x.DeltaMu.Actual)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0, 1, 1, 1.5f
            });

        _log.Snapshots.Select(x => x.DeltaMu.Expected)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0, 1, 1, 1.0f
            });
        _log.Snapshots.Select(x => x.DeltaMu.Error)
            .ToArray()
            .ShouldBeEquivalentTo(new float[]
            {
                0, 0, 0, 0.5f
            });
    }
}