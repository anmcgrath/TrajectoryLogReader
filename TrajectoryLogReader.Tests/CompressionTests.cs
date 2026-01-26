using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.IO;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class CompressionTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"CompressionTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    #region Round-Trip Tests with Real Files

    [Test]
    public void CompressAndDecompress_AnonFile0_RoundTripsWithinTolerance()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = CompressedLogReader.Read(compressedPath);

        // Use tolerance of 0.02 to account for quantization
        // Small axes (MLC, jaws): 0.001 cm resolution -> 0.002 tolerance
        // Large axes (couch): 0.01 unit resolution -> 0.02 tolerance
        // MU/ControlPoint: 0.001 unit resolution -> 0.002 tolerance
        // Angles: 0.01 degree resolution -> 0.02 tolerance
        var result = TrajectoryLogComparer.Compare(original, decompressed, floatTolerance: 0.01f);

        result.AreEqual.ShouldBeTrue(result.ToString());
    }

    [Test]
    public void CompressAndDecompress_AnonFile1_RoundTripsWithinTolerance()
    {
        var originalPath = TestFiles.GetPath("AnonFile1.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = CompressedLogReader.Read(compressedPath);

        var result = TrajectoryLogComparer.Compare(original, decompressed, floatTolerance: 0.02f);

        result.AreEqual.ShouldBeTrue(result.ToString());
    }

    [Test]
    public void CompressAndDecompress_AnonFile2_RoundTripsWithinTolerance()
    {
        var originalPath = TestFiles.GetPath("AnonFile2.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = CompressedLogReader.Read(compressedPath);

        var result = TrajectoryLogComparer.Compare(original, decompressed, floatTolerance: 0.02f);

        result.AreEqual.ShouldBeTrue(result.ToString());
    }

    [Test]
    public void CompressAndDecompress_AnonFile3_RoundTripsWithinTolerance()
    {
        var originalPath = TestFiles.GetPath("AnonFile3.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = CompressedLogReader.Read(compressedPath);

        var result = TrajectoryLogComparer.Compare(original, decompressed, floatTolerance: 0.02f);

        result.AreEqual.ShouldBeTrue(result.ToString());
    }

    #endregion

    #region Compression Ratio Tests

    [Test]
    public void Compress_AnonFile0_AchievesSignificantCompression()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);

        var originalSize = new FileInfo(originalPath).Length;
        var compressedSize = new FileInfo(compressedPath).Length;
        var ratio = (double)compressedSize / originalSize;

        // We expect at least 50% compression (ratio < 0.5)
        ratio.ShouldBeLessThan(0.5, $"Compression ratio {ratio:P1} is worse than expected. Original: {originalSize}, Compressed: {compressedSize}");

        TestContext.WriteLine($"Original: {originalSize:N0} bytes");
        TestContext.WriteLine($"Compressed: {compressedSize:N0} bytes");
        TestContext.WriteLine($"Ratio: {ratio:P1}");
        TestContext.WriteLine($"Savings: {(1 - ratio):P1}");
    }

    [Test]
    public void Compress_AllTestFiles_ReportsCompressionStats()
    {
        var testFiles = new[] { "AnonFile0.bin", "AnonFile1.bin", "AnonFile2.bin", "AnonFile3.bin" };

        foreach (var file in testFiles)
        {
            var originalPath = TestFiles.GetPath(file);
            var compressedPath = Path.Combine(_tempDir, $"{file}.cbin");

            var original = LogReader.ReadBinary(originalPath);
            CompressedLogWriter.Write(original, compressedPath);

            var originalSize = new FileInfo(originalPath).Length;
            var compressedSize = new FileInfo(compressedPath).Length;
            var ratio = (double)compressedSize / originalSize;

            TestContext.WriteLine($"{file}: {originalSize:N0} -> {compressedSize:N0} ({ratio:P1})");

            // All files should achieve at least 40% compression
            ratio.ShouldBeLessThan(0.6, $"{file}: Compression ratio {ratio:P1} is worse than expected");
        }
    }

    #endregion

    #region Header and Metadata Preservation Tests

    [Test]
    public void CompressAndDecompress_PreservesHeader()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = CompressedLogReader.Read(compressedPath);

        decompressed.Header.SamplingIntervalInMS.ShouldBe(original.Header.SamplingIntervalInMS);
        decompressed.Header.NumAxesSampled.ShouldBe(original.Header.NumAxesSampled);
        decompressed.Header.NumberOfSnapshots.ShouldBe(original.Header.NumberOfSnapshots);
        decompressed.Header.NumberOfSubBeams.ShouldBe(original.Header.NumberOfSubBeams);
        decompressed.Header.MlcModel.ShouldBe(original.Header.MlcModel);
        decompressed.Header.AxisScale.ShouldBe(original.Header.AxisScale);
        decompressed.Header.IsTruncated.ShouldBe(original.Header.IsTruncated);

        for (int i = 0; i < original.Header.NumAxesSampled; i++)
        {
            decompressed.Header.AxesSampled[i].ShouldBe(original.Header.AxesSampled[i]);
            decompressed.Header.SamplesPerAxis[i].ShouldBe(original.Header.SamplesPerAxis[i]);
        }
    }

    [Test]
    public void CompressAndDecompress_PreservesMetaData()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = CompressedLogReader.Read(compressedPath);

        decompressed.MetaData.PatientId.ShouldBe(original.MetaData.PatientId);
        decompressed.MetaData.PlanName.ShouldBe(original.MetaData.PlanName);
        decompressed.MetaData.PlanUID.ShouldBe(original.MetaData.PlanUID);
        decompressed.MetaData.Energy.ShouldBe(original.MetaData.Energy);
        decompressed.MetaData.BeamName.ShouldBe(original.MetaData.BeamName);
        decompressed.MetaData.MUPlanned.ShouldBe(original.MetaData.MUPlanned, 0.001);
        decompressed.MetaData.MURemaining.ShouldBe(original.MetaData.MURemaining, 0.001);
    }

    [Test]
    public void CompressAndDecompress_PreservesSubBeams()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = CompressedLogReader.Read(compressedPath);

        decompressed.SubBeams.Count.ShouldBe(original.SubBeams.Count);

        for (int i = 0; i < original.SubBeams.Count; i++)
        {
            decompressed.SubBeams[i].ControlPoint.ShouldBe(original.SubBeams[i].ControlPoint);
            decompressed.SubBeams[i].MU.ShouldBe(original.SubBeams[i].MU, 0.001f);
            decompressed.SubBeams[i].RadTime.ShouldBe(original.SubBeams[i].RadTime, 0.001f);
            decompressed.SubBeams[i].SequenceNumber.ShouldBe(original.SubBeams[i].SequenceNumber);
            decompressed.SubBeams[i].Name.ShouldBe(original.SubBeams[i].Name);
        }
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public void CompressedLogReader_NullFilePath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => CompressedLogReader.Read((string)null!));
    }

    [Test]
    public void CompressedLogReader_NullStream_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => CompressedLogReader.Read((Stream)null!));
    }

    [Test]
    public void CompressedLogReader_FileNotFound_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent.cbin");
        Should.Throw<FileNotFoundException>(() => CompressedLogReader.Read(nonExistentPath));
    }

    [Test]
    public void CompressedLogReader_WrongSignature_ThrowsInvalidDataException()
    {
        // Try to read an uncompressed file with the compressed reader
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var ex = Should.Throw<InvalidDataException>(() => CompressedLogReader.Read(originalPath));
        ex.Message.ShouldContain("Invalid file signature");
    }

    [Test]
    public void CompressedLogWriter_NullLog_ThrowsArgumentNullException()
    {
        var filePath = Path.Combine(_tempDir, "test.cbin");
        Should.Throw<ArgumentNullException>(() => CompressedLogWriter.Write(null!, filePath));
    }

    [Test]
    public void CompressedLogWriter_NullFilePath_ThrowsArgumentNullException()
    {
        var original = LogReader.ReadBinary(TestFiles.GetPath("AnonFile0.bin"));
        Should.Throw<ArgumentNullException>(() => CompressedLogWriter.Write(original, (string)null!));
    }

    #endregion

    #region Clinical Tolerance Tests

    [Test]
    public void CompressAndDecompress_MlcPositions_WithinClinicalTolerance()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = CompressedLogReader.Read(compressedPath);

        // Clinical tolerance for MLC is typically 0.5mm = 0.05cm
        const float clinicalTolerance = 0.05f;

        var mlcAxisIndex = original.Header.GetAxisIndex(Axis.MLC);
        if (mlcAxisIndex >= 0)
        {
            var origMlc = original.AxisData[mlcAxisIndex];
            var decompMlc = decompressed.AxisData[mlcAxisIndex];

            float maxDiff = 0;
            for (int i = 0; i < origMlc.Data.Length; i++)
            {
                var diff = Math.Abs(origMlc.Data[i] - decompMlc.Data[i]);
                maxDiff = Math.Max(maxDiff, diff);
            }

            TestContext.WriteLine($"Max MLC position difference: {maxDiff:F4} cm ({maxDiff * 10:F2} mm)");
            maxDiff.ShouldBeLessThan(clinicalTolerance, $"MLC position difference {maxDiff} cm exceeds clinical tolerance of {clinicalTolerance} cm");
        }
    }

    [Test]
    public void CompressAndDecompress_GantryAngle_WithinClinicalTolerance()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = CompressedLogReader.Read(compressedPath);

        // Clinical tolerance for gantry is typically 0.1 degrees
        const float clinicalTolerance = 0.1f;

        var gantryAxisIndex = original.Header.GetAxisIndex(Axis.GantryRtn);
        if (gantryAxisIndex >= 0)
        {
            var origGantry = original.AxisData[gantryAxisIndex];
            var decompGantry = decompressed.AxisData[gantryAxisIndex];

            float maxDiff = 0;
            for (int i = 0; i < origGantry.Data.Length; i++)
            {
                var diff = Math.Abs(origGantry.Data[i] - decompGantry.Data[i]);
                // Handle wraparound for angles near 0/360
                if (diff > 180) diff = 360 - diff;
                maxDiff = Math.Max(maxDiff, diff);
            }

            TestContext.WriteLine($"Max Gantry angle difference: {maxDiff:F4} degrees");
            maxDiff.ShouldBeLessThan(clinicalTolerance, $"Gantry angle difference {maxDiff} degrees exceeds clinical tolerance of {clinicalTolerance} degrees");
        }
    }

    #endregion

    #region Async Tests

    [Test]
    public async Task CompressedLogReaderAsync_RoundTripsCorrectly()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");

        var original = await LogReader.ReadBinaryAsync(originalPath);
        CompressedLogWriter.Write(original, compressedPath);
        var decompressed = await CompressedLogReader.ReadAsync(compressedPath);

        var result = TrajectoryLogComparer.Compare(original, decompressed, floatTolerance: 0.02f);
        result.AreEqual.ShouldBeTrue(result.ToString());
    }

    #endregion

    #region GZip Compression Tests

    [Test]
    public void CompressWithGzip_AchievesBetterCompression()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var compressedPath = Path.Combine(_tempDir, "compressed.cbin");
        var gzipPath = Path.Combine(_tempDir, "compressed.cbin.gz");

        var original = LogReader.ReadBinary(originalPath);

        // Write without GZip
        CompressedLogWriter.Write(original, compressedPath, useGzip: false);

        // Write with GZip
        CompressedLogWriter.Write(original, gzipPath, useGzip: true);

        var originalSize = new FileInfo(originalPath).Length;
        var compressedSize = new FileInfo(compressedPath).Length;
        var gzipSize = new FileInfo(gzipPath).Length;

        var deltaRatio = (double)compressedSize / originalSize;
        var gzipRatio = (double)gzipSize / originalSize;

        TestContext.WriteLine($"Original:     {originalSize:N0} bytes");
        TestContext.WriteLine($"Delta only:   {compressedSize:N0} bytes ({deltaRatio:P1})");
        TestContext.WriteLine($"Delta + GZip: {gzipSize:N0} bytes ({gzipRatio:P1})");
        TestContext.WriteLine($"Additional savings from GZip: {(1 - (double)gzipSize / compressedSize):P1}");

        // GZip should provide additional compression
        gzipSize.ShouldBeLessThan(compressedSize);
    }

    [Test]
    public void CompressWithGzip_RoundTripsCorrectly()
    {
        var originalPath = TestFiles.GetPath("AnonFile0.bin");
        var gzipPath = Path.Combine(_tempDir, "compressed.cbin.gz");

        var original = LogReader.ReadBinary(originalPath);
        CompressedLogWriter.Write(original, gzipPath, useGzip: true);

        // Reader should auto-detect GZip
        var decompressed = CompressedLogReader.Read(gzipPath);

        var result = TrajectoryLogComparer.Compare(original, decompressed, floatTolerance: 0.02f);
        result.AreEqual.ShouldBeTrue(result.ToString());
    }

    [Test]
    public void CompressWithGzip_AllTestFiles_ReportsCompressionStats()
    {
        var testFiles = new[] { "AnonFile0.bin", "AnonFile1.bin", "AnonFile2.bin", "AnonFile3.bin" };

        TestContext.WriteLine("File                Original      Delta       Delta+GZip  Total Savings");
        TestContext.WriteLine("----                --------      -----       ----------  -------------");

        foreach (var file in testFiles)
        {
            var originalPath = TestFiles.GetPath(file);
            var gzipPath = Path.Combine(_tempDir, $"{file}.gz");

            var original = LogReader.ReadBinary(originalPath);
            CompressedLogWriter.Write(original, gzipPath, useGzip: true);

            var originalSize = new FileInfo(originalPath).Length;
            var gzipSize = new FileInfo(gzipPath).Length;
            var ratio = (double)gzipSize / originalSize;

            TestContext.WriteLine($"{file,-19} {originalSize,10:N0}  {"-",10}  {gzipSize,10:N0}  {(1 - ratio):P1}");

            // Verify round-trip
            var decompressed = CompressedLogReader.Read(gzipPath);
            var result = TrajectoryLogComparer.Compare(original, decompressed, floatTolerance: 0.02f);
            result.AreEqual.ShouldBeTrue($"{file}: {result}");
        }
    }

    #endregion
}
