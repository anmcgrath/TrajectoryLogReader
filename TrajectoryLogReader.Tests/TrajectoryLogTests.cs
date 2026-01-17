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
}