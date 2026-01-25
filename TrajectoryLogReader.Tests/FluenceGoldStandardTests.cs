using Shouldly;
using TrajectoryLogReader.Fluence;
using TrajectoryLogReader.IO;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Tests.Serialization;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class FluenceGoldStandardTests
{
    private static IEnumerable<string> GetTestBinFiles()
    {
        if (!Directory.Exists(TestFiles.Directory))
            yield break;

        var files = Directory.GetFiles(TestFiles.Directory).Where(x => x.EndsWith(".bin"));

        foreach (var binFile in files)
        {
            var fileName = Path.GetFileName(binFile);
            var expectedFluencePath = Path.Combine(TestFiles.Directory, $"{fileName}.expected.fluence");
            var actualFluencePath = Path.Combine(TestFiles.Directory, $"{fileName}.actual.fluence");

            if (File.Exists(expectedFluencePath) || File.Exists(actualFluencePath))
                yield return binFile;
        }
    }

    [TestCaseSource(nameof(GetTestBinFiles))]
    public void ExpectedFluence_MatchesGoldStandard(string binFilePath)
    {
        var fileName = Path.GetFileName(binFilePath);
        var expectedFluencePath = Path.Combine(TestFiles.Directory, $"{fileName}.expected.fluence");

        if (!File.Exists(expectedFluencePath))
            Assert.Ignore($"No expected fluence gold standard file found for {fileName}");

        var log = LogReader.ReadBinary(binFilePath);
        var goldStandard = FluenceSerializer.DeserializeFromFile(expectedFluencePath);

        var calculated = log.CreateFluence(goldStandard.Options, RecordType.ExpectedPosition);

        CompareFluenceGrids(calculated.Grid, goldStandard.Grid, fileName, "expected");
    }

    [TestCaseSource(nameof(GetTestBinFiles))]
    public void ActualFluence_MatchesGoldStandard(string binFilePath)
    {
        var fileName = Path.GetFileName(binFilePath);
        var actualFluencePath = Path.Combine(TestFiles.Directory, $"{fileName}.actual.fluence");

        if (!File.Exists(actualFluencePath))
            Assert.Ignore($"No actual fluence gold standard file found for {fileName}");

        var log = LogReader.ReadBinary(binFilePath);
        var goldStandard = FluenceSerializer.DeserializeFromFile(actualFluencePath);

        var calculated = log.CreateFluence(goldStandard.Options, RecordType.ActualPosition);

        CompareFluenceGrids(calculated.Grid, goldStandard.Grid, fileName, "actual");
    }

    private static void CompareFluenceGrids(GridF calculated, GridF expected, string fileName, string fluenceType)
    {
        calculated.Cols.ShouldBe(expected.Cols, $"Column count mismatch for {fileName} {fluenceType} fluence");
        calculated.Rows.ShouldBe(expected.Rows, $"Row count mismatch for {fileName} {fluenceType} fluence");
        calculated.Bounds.X.ShouldBe(expected.Bounds.X, 1e-6,
            $"Bounds.X mismatch for {fileName} {fluenceType} fluence");
        calculated.Bounds.Y.ShouldBe(expected.Bounds.Y, 1e-6,
            $"Bounds.Y mismatch for {fileName} {fluenceType} fluence");
        calculated.Bounds.Width.ShouldBe(expected.Bounds.Width, 1e-6,
            $"Bounds.Width mismatch for {fileName} {fluenceType} fluence");
        calculated.Bounds.Height.ShouldBe(expected.Bounds.Height, 1e-6,
            $"Bounds.Height mismatch for {fileName} {fluenceType} fluence");

        for (int i = 0; i < expected.Data.Length; i++)
        {
            var row = i / expected.Cols;
            var col = i % expected.Cols;
            calculated.Data[i].ShouldBe(expected.Data[i], 1f,
                $"Data mismatch at [{row}, {col}] for {fileName} {fluenceType} fluence");
        }
    }
}