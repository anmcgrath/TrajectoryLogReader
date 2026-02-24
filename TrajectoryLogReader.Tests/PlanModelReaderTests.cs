using FellowOakDicom;
using Shouldly;
using TrajectoryLogReader.DICOM;
using TrajectoryLogReader.DICOM.Plan;

namespace TrajectoryLogReader.Tests;

public class PlanModelReaderTests
{
    [Test]
    public void Read_WhenPrimaryFluenceModeSequenceMissing_DefaultsToStandard()
    {
        var plan = ReadPlan(CreatePlanDataset(CreateBeamDataset()));

        plan.Beams.Count.ShouldBe(1);
        plan.Beams[0].PrimaryFluenceMode.ShouldBe(FluenceMode.Standard);
    }

    [Test]
    public void Read_WhenBeamSequenceMissing_ReturnsEmptyBeamList()
    {
        var plan = ReadPlan(CreatePlanDataset());

        plan.Beams.ShouldBeEmpty();
    }

    [Test]
    public void Read_WhenControlPointContainsMlcY_ParsesLeafData()
    {
        var mlcy = CreateBeamLimitingDevicePosition("MLCY", 1f, 2f, 3f, 4f);
        var plan = ReadPlan(CreatePlanDataset(CreateBeamDataset(mlcy)));

        var cpData = plan.Beams[0].ControlPoints[0];
        cpData.MlcData.ShouldNotBeNull();
        cpData.MlcData[0, 0].ShouldBe(1f);
        cpData.MlcData[0, 1].ShouldBe(2f);
        cpData.MlcData[1, 0].ShouldBe(3f);
        cpData.MlcData[1, 1].ShouldBe(4f);
    }

    [Test]
    public void Read_WhenControlPointContainsUnknownBeamLimitingDeviceType_DoesNotThrow()
    {
        var unknown = CreateBeamLimitingDevicePosition("UNKNOWN", -10f, 10f);
        var plan = ReadPlan(CreatePlanDataset(CreateBeamDataset(unknown)));

        plan.Beams[0].ControlPoints.Count.ShouldBe(1);
    }

    [Test]
    public void Read_WhenBeamHasNoMlcDefinition_LeavesMlcNull()
    {
        var plan = ReadPlan(CreatePlanDataset(CreateBeamDataset()));

        plan.Beams[0].Mlc.ShouldBeNull();
    }

    private static PlanModel ReadPlan(DicomDataset dataset)
    {
        var file = new DicomFile(dataset);
        using var ms = new MemoryStream();
        file.Save(ms);
        ms.Position = 0;
        return PlanModelReader.Read(ms);
    }

    private static DicomDataset CreatePlanDataset(DicomDataset? beamDataset = null)
    {
        var dataset = new DicomDataset();
        dataset.Add(DicomTag.SOPClassUID, DicomUID.RTPlanStorage);
        dataset.Add(DicomTag.SOPInstanceUID, DicomUID.Generate());
        dataset.Add(DicomTag.StudyInstanceUID, DicomUID.Generate());
        dataset.Add(DicomTag.SeriesInstanceUID, DicomUID.Generate());
        dataset.Add(DicomTag.Modality, "RTPLAN");

        if (beamDataset != null)
        {
            dataset.Add(new DicomSequence(DicomTag.BeamSequence, beamDataset));
        }

        return dataset;
    }

    private static DicomDataset CreateBeamDataset(params DicomDataset[] beamLimitingDevicePositions)
    {
        var beam = new DicomDataset();
        beam.Add(DicomTag.BeamName, "Beam 1");
        beam.Add(DicomTag.BeamNumber, 1);
        beam.Add(DicomTag.PrimaryDosimeterUnit, "MU");
        beam.Add(DicomTag.NumberOfControlPoints, 1);
        beam.Add(DicomTag.RadiationType, "PHOTON");
        beam.Add(DicomTag.BeamType, "STATIC");

        var cp = new DicomDataset();
        cp.Add(DicomTag.CumulativeMetersetWeight, 0f);

        if (beamLimitingDevicePositions.Length > 0)
        {
            cp.Add(new DicomSequence(DicomTag.BeamLimitingDevicePositionSequence, beamLimitingDevicePositions));
        }

        beam.Add(new DicomSequence(DicomTag.ControlPointSequence, cp));
        return beam;
    }

    private static DicomDataset CreateBeamLimitingDevicePosition(string type, params float[] positions)
    {
        var dataset = new DicomDataset();
        dataset.Add(DicomTag.RTBeamLimitingDeviceType, type);
        dataset.Add(DicomTag.LeafJawPositions, positions);
        return dataset;
    }
}