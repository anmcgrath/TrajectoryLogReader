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
}