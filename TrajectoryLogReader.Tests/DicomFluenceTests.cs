using Shouldly;
using TrajectoryLogReader.DICOM.FluenceAdapters;
using TrajectoryLogReader.DICOM.Plan;

namespace TrajectoryLogReader.Tests;

public class DicomFluenceTests
{
    [Test]
    [TestCase(1)]
    [TestCase(0.5)]
    [TestCase(0.1)]
    [TestCase(2)]
    public void Beam_Collection_Adapter_Gets_AllMu(double cpDelta)
    {
        var beam = new BeamModel()
        {
            MU = 50,
            ControlPoints = new List<ControlPointData>()
            {
                new() { CumulativeMetersetWeight = 0 },
                new() { CumulativeMetersetWeight = .2f },
                new() { CumulativeMetersetWeight = .4f },
                new() { CumulativeMetersetWeight = .5f },
                new() { CumulativeMetersetWeight = .8f },
                new() { CumulativeMetersetWeight = 1 },
            }
        };

        var adapter = new BeamCollectionAdapter(beam, cpDelta);
        adapter.GetFieldData().Sum(x => x.DeltaMu).ShouldBe(beam.MU, 0.0001);
    }

    [Test]
    [TestCase(1)]
    [TestCase(0.5)]
    public void Beam_Collection_Adapter_Normalises_By_Final_Cumulative_Meterset_Weight(double cpDelta)
    {
        var beam = new BeamModel()
        {
            MU = 50,
            FinalCumulativeMetersetWeight = 100,
            ControlPoints = new List<ControlPointData>()
            {
                new() { CumulativeMetersetWeight = 0 },
                new() { CumulativeMetersetWeight = 40 },
                new() { CumulativeMetersetWeight = 100 },
            }
        };

        var adapter = new BeamCollectionAdapter(beam, cpDelta);
        adapter.GetFieldData().Sum(x => x.DeltaMu).ShouldBe(beam.MU, 0.0001);
    }

    [Test]
    public void Beam_Cumulative_Mu_Falls_Back_To_Last_Control_Point_Weight()
    {
        var beam = new BeamModel()
        {
            MU = 200,
            ControlPoints = new List<ControlPointData>()
            {
                new() { CumulativeMetersetWeight = 0 },
                new() { CumulativeMetersetWeight = 200 },
            }
        };

        beam.GetCumulativeMu(50).ShouldBe(50, 0.0001);
    }
}
