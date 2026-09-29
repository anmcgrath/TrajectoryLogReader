using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class TrajectoryLogTests
{
    [Test]
    public void Anonymize_RemovesSensitiveInformation()
    {
        // Arrange
        var log = new TrajectoryLog
        {
            MetaData = new MetaData
            {
                PatientId = "12345",
                PlanName = "Test Plan",
                PlanUID = "1.2.3.4",
                SOPInstanceUID = "5.6.7.8",
                BeamName = "Gantry 1"
            },
            FilePath = "/path/to/patient/log.bin",
            SubBeams = new List<SubBeam>
            {
                new SubBeam(null!) { Name = "SubBeam 1" },
                new SubBeam(null!) { Name = "SubBeam 2" }
            }
        };

        // Act
        log.Anonymize(new AnonymizationOptions()
        {
            SubBeamNameSelector = i => "Anonymized"
        });

        // Assert
        log.MetaData.PatientId.ShouldBe("Anonymized");
        log.MetaData.PlanName.ShouldBe("Anonymized");
        log.MetaData.PlanUID.ShouldBe("Anonymized");
        log.MetaData.SOPInstanceUID.ShouldBe("Anonymized");
        log.MetaData.BeamName.ShouldBe("Anonymized");
        log.FilePath.ShouldBe("Anonymized");
        foreach (var subBeam in log.SubBeams)
        {
            subBeam.Name.ShouldBe("Anonymized");
        }
    }

    [Test]
    public void Anonymize_WithCustomOptions_UsesCustomValues()
    {
        // Arrange
        var log = new TrajectoryLog
        {
            MetaData = new MetaData
            {
                PatientId = "12345",
                PlanName = "Test Plan",
                PlanUID = "1.2.3.4",
                SOPInstanceUID = "5.6.7.8",
                BeamName = "Gantry 1"
            },
            FilePath = "/path/to/patient/log.bin",
            SubBeams = new List<SubBeam>
            {
                new SubBeam(null!) { Name = "SubBeam 1" }
            }
        };

        var options = new AnonymizationOptions
        {
            PatientId = "P-001",
            PlanName = "P-Name",
            PlanUID = "P-UID",
            SOPInstanceUID = "S-UID",
            BeamName = "B-Name",
            FilePath = "F-Path",
            SubBeamNameSelector = i => "SB-Name"
        };

        // Act
        log.Anonymize(options);

        // Assert
        log.MetaData.PatientId.ShouldBe("P-001");
        log.MetaData.PlanName.ShouldBe("P-Name");
        log.MetaData.PlanUID.ShouldBe("P-UID");
        log.MetaData.SOPInstanceUID.ShouldBe("S-UID");
        log.MetaData.BeamName.ShouldBe("B-Name");
        log.FilePath.ShouldBe("F-Path");
        log.SubBeams[0].Name.ShouldBe("SB-Name");
    }

    [Test]
    public void SubBeam_starts_once_the_control_point_moves_past_the_previous_beams_end()
    {
        // The first beam reaches control point 1 and dwells there (snapshots 2-4) while the
        // actual MU catches up with the expected; that tail belongs to the first beam.
        var expectedCps = new[] { 0f, 0.5f, 1f, 1f, 1f, 2f, 2.5f, 3f };
        var cpData = new AxisData(expectedCps.Length, 2);
        for (int i = 0; i < expectedCps.Length; i++)
        {
            cpData.Data[i * 2] = expectedCps[i];
            cpData.Data[i * 2 + 1] = expectedCps[i];
        }

        var log = new TrajectoryLog
        {
            Header = new Header
            {
                Version = 5,
                SamplingIntervalInMS = 20,
                NumAxesSampled = 1,
                AxesSampled = new[] { Axis.ControlPoint },
                SamplesPerAxis = new[] { 1 },
                NumberOfSubBeams = 2,
                NumberOfSnapshots = expectedCps.Length
            },
            MetaData = new MetaData(),
            AxisData = new[] { cpData }
        };
        log.SubBeams = new List<SubBeam>
        {
            new SubBeam(log) { ControlPoint = 0, SequenceNumber = 0 },
            new SubBeam(log) { ControlPoint = 1, SequenceNumber = 1 }
        };

        log.SubBeams[0].StartIndex.ShouldBe(0);
        log.SubBeams[0].EndIndex.ShouldBe(4);
        log.SubBeams[1].StartIndex.ShouldBe(5);
        log.SubBeams[1].EndIndex.ShouldBe(7);
    }
}
