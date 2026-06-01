using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.DICOM.Plan;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class ControlPointInterpolatorTests
{
    private static ControlPointData Cp(
        int index,
        float gantry,
        float collimator,
        ControlPointRotationDirection? gantryDirection = null,
        ControlPointRotationDirection? collimatorDirection = null)
    {
        return new ControlPointData
        {
            ControlPointIndex = index,
            CumulativeMetersetWeight = index,
            X1 = -50,
            X2 = 50,
            Y1 = -50,
            Y2 = 50,
            GantryAngle = gantry,
            GantryRotationDirection = gantryDirection,
            CollimatorAngle = collimator,
            CollimatorRotationDirection = collimatorDirection,
            MlcData = new float[2, 1]
        };
    }

    [Test]
    public void Interpolate_Collimator_Across_360_Seam_Uses_Shortest_Arc()
    {
        var cp0 = Cp(0, 0, 350, collimatorDirection: ControlPointRotationDirection.Clockwise);
        var cp1 = Cp(1, 0, 10);

        var mid = ControlPointInterpolator.Interpolate(cp0, cp1, 0.5);

        var coll = mid.CollimatorAngle!.Value;
        Math.Min(coll, 360f - coll).ShouldBe(0f, 0.001);
    }

    [Test]
    public void Interpolate_Gantry_Across_360_Seam_Uses_Shortest_Arc()
    {
        var cp0 = Cp(0, 358, 0, gantryDirection: ControlPointRotationDirection.Clockwise);
        var cp1 = Cp(1, 2, 0);

        var mid = ControlPointInterpolator.Interpolate(cp0, cp1, 0.5);

        var gantry = mid.GantryAngle!.Value;
        Math.Min(gantry, 360f - gantry).ShouldBe(0f, 0.001);
    }

    [Test]
    public void Interpolate_Across_360_Seam_Honours_CounterClockwise_Direction()
    {
        var cp0 = Cp(0, 10, 10, ControlPointRotationDirection.CounterClockwise,
            ControlPointRotationDirection.CounterClockwise);
        var cp1 = Cp(1, 350, 350);

        var mid = ControlPointInterpolator.Interpolate(cp0, cp1, 0.5);

        var gantry = mid.GantryAngle!.Value;
        var coll = mid.CollimatorAngle!.Value;
        Math.Min(gantry, 360f - gantry).ShouldBe(0f, 0.001);
        Math.Min(coll, 360f - coll).ShouldBe(0f, 0.001);
    }

    [Test]
    public void Interpolate_Across_360_Seam_Honours_Clockwise_Direction()
    {
        var cp0 = Cp(0, 10, 10, ControlPointRotationDirection.Clockwise, ControlPointRotationDirection.Clockwise);
        var cp1 = Cp(1, 350, 350);

        var mid = ControlPointInterpolator.Interpolate(cp0, cp1, 0.5);

        mid.GantryAngle!.Value.ShouldBe(180f, 0.001);
        mid.CollimatorAngle!.Value.ShouldBe(180f, 0.001);
    }

    [Test]
    public void Interpolate_Across_360_Seam_Without_Direction_Uses_Linear_Angle()
    {
        var cp0 = Cp(0, 350, 350);
        var cp1 = Cp(1, 10, 10);

        var mid = ControlPointInterpolator.Interpolate(cp0, cp1, 0.5);

        mid.GantryAngle!.Value.ShouldBe(180f, 0.001);
        mid.CollimatorAngle!.Value.ShouldBe(180f, 0.001);
    }

    [Test]
    public void Interpolate_RotationDirection_None_Holds_Start_Angle()
    {
        var cp0 = Cp(0, 10, 20, ControlPointRotationDirection.None, ControlPointRotationDirection.None);
        var cp1 = Cp(1, 350, 320);

        var mid = ControlPointInterpolator.Interpolate(cp0, cp1, 0.5);

        mid.GantryAngle!.Value.ShouldBe(10f, 0.001);
        mid.CollimatorAngle!.Value.ShouldBe(20f, 0.001);
    }

    [Test]
    public void Interpolate_Within_Range_Is_Unchanged()
    {
        // Non-wrapping case should behave like a plain linear interpolation.
        var cp0 = Cp(0, 90, 45);
        var cp1 = Cp(1, 100, 55);

        var mid = ControlPointInterpolator.Interpolate(cp0, cp1, 0.5);

        mid.GantryAngle!.Value.ShouldBe(95f, 0.001);
        mid.CollimatorAngle!.Value.ShouldBe(50f, 0.001);
    }
}
