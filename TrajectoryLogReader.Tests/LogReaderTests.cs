using System.Text;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.IO;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class LogReaderTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LogReaderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    #region Null and Missing File Tests

    [Test]
    public void ReadBinary_NullFilePath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => LogReader.ReadBinary((string)null!));
    }

    [Test]
    public async Task ReadBinaryAsync_NullFilePath_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => LogReader.ReadBinaryAsync((string)null!));
    }

    [Test]
    public void ReadBinary_NullStream_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => LogReader.ReadBinary((Stream)null!));
    }

    [Test]
    public async Task ReadBinaryAsync_NullStream_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => LogReader.ReadBinaryAsync((Stream)null!));
    }

    [Test]
    public void ReadBinary_FileNotFound_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent.bin");
        Should.Throw<FileNotFoundException>(() => LogReader.ReadBinary(nonExistentPath));
    }

    [Test]
    public async Task ReadBinaryAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent.bin");
        await Should.ThrowAsync<FileNotFoundException>(() => LogReader.ReadBinaryAsync(nonExistentPath));
    }

    #endregion

    #region Invalid Signature Tests

    [Test]
    public void ReadBinary_InvalidSignature_ThrowsInvalidDataException()
    {
        var filePath = CreateTestFile("INVALID_SIGNATURE!");
        var ex = Should.Throw<InvalidDataException>(() => LogReader.ReadBinary(filePath));
        ex.Message.ShouldContain("Invalid file signature");
    }

    [Test]
    public void ReadBinary_EmptyFile_ThrowsEndOfStreamException()
    {
        var filePath = CreateTestFile("");
        Should.Throw<EndOfStreamException>(() => LogReader.ReadBinary(filePath));
    }

    [Test]
    public void ReadBinary_TruncatedSignature_ThrowsEndOfStreamException()
    {
        var filePath = CreateTestFile("VOSTL"); // Only 5 bytes, need 16
        Should.Throw<EndOfStreamException>(() => LogReader.ReadBinary(filePath));
    }

    #endregion

    #region Truncated File Tests

    [Test]
    public void ReadBinary_TruncatedAfterSignature_ThrowsEndOfStreamException()
    {
        // Create file with valid signature but truncated before version
        var data = new byte[16];
        Encoding.UTF8.GetBytes("VOSTL").CopyTo(data, 0);
        var filePath = CreateTestFileFromBytes(data);

        Should.Throw<EndOfStreamException>(() => LogReader.ReadBinary(filePath));
    }

    [Test]
    public void ReadBinary_InvalidVersion_ThrowsInvalidDataException()
    {
        // Create file with valid signature but invalid version string
        var data = new byte[32];
        Encoding.UTF8.GetBytes("VOSTL").CopyTo(data, 0);
        Encoding.UTF8.GetBytes("NOT_A_NUMBER").CopyTo(data, 16);
        var filePath = CreateTestFileFromBytes(data);

        var ex = Should.Throw<InvalidDataException>(() => LogReader.ReadBinary(filePath));
        ex.Message.ShouldContain("Invalid version format");
    }

    #endregion

    #region Invalid Header Values Tests

    [Test]
    public void ReadBinary_NegativeNumAxesSampled_ThrowsInvalidDataException()
    {
        var data = CreateMinimalHeaderWithAxesCount(-1);
        var filePath = CreateTestFileFromBytes(data);

        var ex = Should.Throw<InvalidDataException>(() => LogReader.ReadBinary(filePath));
        ex.Message.ShouldContain("Invalid number of axes");
    }

    [Test]
    public void ReadBinary_TooManyAxesSampled_ThrowsInvalidDataException()
    {
        var data = CreateMinimalHeaderWithAxesCount(5000);
        var filePath = CreateTestFileFromBytes(data);

        var ex = Should.Throw<InvalidDataException>(() => LogReader.ReadBinary(filePath));
        ex.Message.ShouldContain("Invalid number of axes");
    }

    [Test]
    public void ReadBinary_NegativeNumberOfSnapshots_ThrowsInvalidDataException()
    {
        var data = CreateMinimalHeaderWithSnapshotCount(-1);
        var filePath = CreateTestFileFromBytes(data);

        var ex = Should.Throw<InvalidDataException>(() => LogReader.ReadBinary(filePath));
        ex.Message.ShouldContain("Invalid number of snapshots");
    }

    [Test]
    public void ReadBinary_TooManySnapshots_ThrowsInvalidDataException()
    {
        var data = CreateMinimalHeaderWithSnapshotCount(100_000_000);
        var filePath = CreateTestFileFromBytes(data);

        var ex = Should.Throw<InvalidDataException>(() => LogReader.ReadBinary(filePath));
        ex.Message.ShouldContain("Invalid number of snapshots");
    }

    #endregion

    #region Stream Tests

    [Test]
    public void ReadBinary_StreamWithInvalidSignature_ThrowsInvalidDataException()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("INVALID_SIGNATURE!"));
        var ex = Should.Throw<InvalidDataException>(() => LogReader.ReadBinary(ms));
        ex.Message.ShouldContain("Invalid file signature");
    }

    [Test]
    public async Task ReadBinaryAsync_StreamWithInvalidSignature_ThrowsInvalidDataException()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("INVALID_SIGNATURE!"));
        var ex = await Should.ThrowAsync<InvalidDataException>(() => LogReader.ReadBinaryAsync(ms));
        ex.Message.ShouldContain("Invalid file signature");
    }

    #endregion

    #region Helper Methods

    private string CreateTestFile(string content)
    {
        var filePath = Path.Combine(_tempDir, $"test_{Guid.NewGuid()}.bin");
        File.WriteAllText(filePath, content);
        return filePath;
    }

    private string CreateTestFileFromBytes(byte[] data)
    {
        var filePath = Path.Combine(_tempDir, $"test_{Guid.NewGuid()}.bin");
        File.WriteAllBytes(filePath, data);
        return filePath;
    }

    private byte[] CreateMinimalHeaderWithAxesCount(int numAxes)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Signature (16 bytes)
        var sig = new byte[16];
        Encoding.UTF8.GetBytes("VOSTL").CopyTo(sig, 0);
        bw.Write(sig);

        // Version (16 bytes)
        var ver = new byte[16];
        Encoding.UTF8.GetBytes("5.0").CopyTo(ver, 0);
        bw.Write(ver);

        // Header size
        bw.Write(1024);

        // Sampling interval
        bw.Write(20);

        // NumAxesSampled (this is what we're testing)
        bw.Write(numAxes);

        return ms.ToArray();
    }

    private byte[] CreateMinimalHeaderWithSnapshotCount(int numSnapshots)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Signature (16 bytes)
        var sig = new byte[16];
        Encoding.UTF8.GetBytes("VOSTL").CopyTo(sig, 0);
        bw.Write(sig);

        // Version (16 bytes)
        var ver = new byte[16];
        Encoding.UTF8.GetBytes("5.0").CopyTo(ver, 0);
        bw.Write(ver);

        // Header size
        bw.Write(1024);

        // Sampling interval
        bw.Write(20);

        // NumAxesSampled (valid)
        bw.Write(1);

        // AxesSampled (1 axis)
        bw.Write(0);

        // SamplesPerAxis (1 axis)
        bw.Write(1);

        // AxisScale
        bw.Write(0);

        // NumberOfSubBeams (valid)
        bw.Write(0);

        // IsTruncated
        bw.Write(0);

        // NumberOfSnapshots (this is what we're testing)
        bw.Write(numSnapshots);

        return ms.ToArray();
    }

    #endregion
}
