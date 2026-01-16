using Shouldly;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Tests;

public class ScaleConversionTests
{
    [Test]
    public void VarianIecToIec()
    {
        Scale.ToIec(AxisScale.ModifiedIEC61217, Axis.GantryRtn, 0).ShouldBe(0);
        Scale.ToIec(AxisScale.ModifiedIEC61217, Axis.CouchRtn, 90).ShouldBe(270);
        Scale.ToIec(AxisScale.ModifiedIEC61217, Axis.CouchRtn, 270).ShouldBe(90);
        Scale.ToIec(AxisScale.ModifiedIEC61217, Axis.CouchVrt, 16).ShouldBe(-16);
        Scale.ToIec(AxisScale.ModifiedIEC61217, Axis.CouchVrt, 1005).ShouldBe(5);
        Scale.ToIec(AxisScale.ModifiedIEC61217, Axis.CouchLat, 985).ShouldBe(-15);
        Scale.ToIec(AxisScale.ModifiedIEC61217, Axis.CouchLat, 15).ShouldBe(15);
    }

    [Test]
    public void IecToVarianIec()
    {
        var vc = new VarianIECScaleConverter();
        vc.FromIec(Axis.GantryRtn, 0).ShouldBe(0);
        vc.FromIec(Axis.GantryRtn, 100).ShouldBe(100);
        vc.FromIec(Axis.GantryRtn, 359).ShouldBe(359);
        vc.FromIec(Axis.CollRtn, 0).ShouldBe(0);
        vc.FromIec(Axis.CollRtn, 100).ShouldBe(100);
        vc.FromIec(Axis.CollRtn, 359).ShouldBe(359);

        vc.FromIec(Axis.CouchRtn, 90).ShouldBe(270);
        vc.FromIec(Axis.CouchRtn, 270).ShouldBe(90);

        vc.FromIec(Axis.CouchLat, -15).ShouldBe(985);
        vc.FromIec(Axis.CouchLat, 15).ShouldBe(15);

        vc.FromIec(Axis.CouchVrt, 15).ShouldBe(1015);
        vc.FromIec(Axis.CouchVrt, -15).ShouldBe(15);
    }

    [Test]
    public void Machine_To_Iec_X1()
    {
        Scale.ToIec(AxisScale.MachineScale, Axis.X1, 10).ShouldBe(-10);
    }

    [Test]
    public void Gantry_Scale_From_ModifiedIEC6()
    {
        Scale.Convert(AxisScale.ModifiedIEC61217, AxisScale.MachineScale, Axis.GantryRtn, 0)
            .ShouldBe(180);
        Scale.Convert(AxisScale.ModifiedIEC61217, AxisScale.MachineScale, Axis.GantryRtn, 359)
            .ShouldBe(181);
    }

    [Test]
    public void Collimator_Scale_From_ModifiedIEC6()
    {
        Scale.Convert(AxisScale.ModifiedIEC61217, AxisScale.MachineScale, Axis.CollRtn, 0)
            .ShouldBe(180);
        Scale.Convert(AxisScale.ModifiedIEC61217, AxisScale.MachineScale, Axis.CollRtn, 359)
            .ShouldBe(181);
    }

    [Test]
    public void Couch_Scale_In_Machine_Scale_From_ModifiedIEC6()
    {
        Scale.Convert(AxisScale.ModifiedIEC61217, AxisScale.MachineScale, Axis.CouchRtn, 0)
            .ShouldBe(180);
        Scale.Convert(AxisScale.ModifiedIEC61217, AxisScale.MachineScale, Axis.CouchRtn, 359)
            .ShouldBe(179);
    }

    [Test]
    public void Gantry_Scale_From_MachineScale()
    {
        Scale.Convert(AxisScale.MachineScale, AxisScale.ModifiedIEC61217, Axis.GantryRtn, 180)
            .ShouldBe(0);
        Scale.Convert(AxisScale.MachineScale, AxisScale.ModifiedIEC61217, Axis.GantryRtn, 181)
            .ShouldBe(359);
    }

    [Test]
    public void Coll_Scale_From_MachineScale()
    {
        Scale.Convert(AxisScale.MachineScale, AxisScale.ModifiedIEC61217, Axis.CollRtn, 180)
            .ShouldBe(0);
        Scale.Convert(AxisScale.MachineScale, AxisScale.ModifiedIEC61217, Axis.CollRtn, 181)
            .ShouldBe(359);
    }

    [Test]
    public void Couch_Scale_From_MachineScale()
    {
        Scale.Convert(AxisScale.MachineScale, AxisScale.ModifiedIEC61217, Axis.CouchRtn, 180)
            .ShouldBe(0);
        Scale.Convert(AxisScale.MachineScale, AxisScale.ModifiedIEC61217, Axis.CouchRtn, 179)
            .ShouldBe(359);
    }

    [Test]
    public void Gantry_Diff_Tests()
    {
        Scale.Delta(AxisScale.ModifiedIEC61217, 359, AxisScale.ModifiedIEC61217, 1, Axis.GantryRtn)
            .ShouldBe(2);
        Scale.Delta(AxisScale.ModifiedIEC61217, 1, AxisScale.ModifiedIEC61217, 359, Axis.GantryRtn)
            .ShouldBe(-2);
        Scale.Delta(AxisScale.ModifiedIEC61217, 9, AxisScale.ModifiedIEC61217, 10, Axis.GantryRtn)
            .ShouldBe(1);
        Scale.Delta(AxisScale.ModifiedIEC61217, 10, AxisScale.ModifiedIEC61217, 9, Axis.GantryRtn)
            .ShouldBe(-1);
    }

    [Test]
    public void MLC_To_Iec_Conversion_Correct()
    {
        // IEc BEV -ve towards X1 and +ve towards X2 for both banks
        // Varian has bank B(X1) reversed so +ve is larger on X1
        Scale.MlcToIec(AxisScale.MachineScale, 0, 1).ShouldBe(1);
        Scale.MlcToIec(AxisScale.MachineScale, 1, 1).ShouldBe(-1);
        Scale.MlcToIec(AxisScale.MachineScaleIsocentric, 0, 1).ShouldBe(1);
        Scale.MlcToIec(AxisScale.MachineScaleIsocentric, 1, 1).ShouldBe(-1);
        Scale.MlcToIec(AxisScale.ModifiedIEC61217, 0, 1).ShouldBe(1);
        Scale.MlcToIec(AxisScale.ModifiedIEC61217, 1, 1).ShouldBe(-1);
    }
}