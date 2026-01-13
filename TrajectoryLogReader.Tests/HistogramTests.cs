using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class HistogramTests
{
    [Test]
    public void FromData_BasicData_ReturnsCorrectCounts()
    {
        // Arrange
        float[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int nBins = 5;
        // min = 0, max = 10, range = 10, binSize = 2
        // Bins: [0, 2), [2, 4), [4, 6), [6, 8), [8, 10]
        // Data points:
        // [0, 2): 0, 1 -> 2
        // [2, 4): 2, 3 -> 2
        // [4, 6): 4, 5 -> 2
        // [6, 8): 6, 7 -> 2
        // [8, 10]: 8, 9, 10 -> 3

        // Act
        var histogram = Histogram.FromData(data, nBins);

        // Assert
        histogram.Counts.ShouldBe(new[] { 2, 2, 2, 2, 3 });
        histogram.BinStarts.ShouldBe(new[] { 0f, 2f, 4f, 6f, 8f });
    }

    [Test]
    public void FromData_AllSameValues_AllInFirstBin()
    {
        // Arrange
        float[] data = { 5, 5, 5, 5, 5 };
        int nBins = 5;
        // min = 5, max = 5, range = 0, binSize = 0
        // (data[i] - min) / binSize = 0 / 0 = NaN -> (int)NaN = 0

        // Act
        var histogram = Histogram.FromData(data, nBins);

        // Assert
        histogram.Counts.ShouldBe(new[] { 5, 0, 0, 0, 0 });
        histogram.BinStarts.ShouldBe(new[] { 5f, 5f, 5f, 5f, 5f });
    }

    [Test]
    public void FromData_SingleValue_ReturnsCorrectCounts()
    {
        // Arrange
        float[] data = { 10f };
        int nBins = 2;

        // Act
        var histogram = Histogram.FromData(data, nBins);

        // Assert
        histogram.Counts.ShouldBe(new[] { 1, 0 });
        histogram.BinStarts.ShouldBe(new[] { 10f, 10f });
    }

    [Test]
    public void FromData_TwoValuesTwoBins_ReturnsOneInEach()
    {
        // Arrange
        float[] data = { 0f, 10f };
        int nBins = 2;
        // min = 0, max = 10, range = 10, binSize = 5
        // 0 -> index 0
        // 10 -> index 2 -> 1

        // Act
        var histogram = Histogram.FromData(data, nBins);

        // Assert
        histogram.Counts.ShouldBe(new[] { 1, 1 });
        histogram.BinStarts.ShouldBe(new[] { 0f, 5f });
    }

    [Test]
    public void FromData_EmptyData_ThrowsException()
    {
        // Arrange
        float[] data = Array.Empty<float>();
        int nBins = 5;

        // Act & Assert
        Should.Throw<IndexOutOfRangeException>(() => Histogram.FromData(data, nBins));
    }

    [Test]
    public void FromData_OneBin_AllValuesInOneBin()
    {
        // Arrange
        float[] data = { 1, 2, 3, 4, 5 };
        int nBins = 1;

        // Act
        var histogram = Histogram.FromData(data, nBins);

        // Assert
        histogram.Counts.ShouldBe(new[] { 5 });
        histogram.BinStarts.ShouldBe(new[] { 1f });
    }

    [Test]
    public void FromData_NegativeValues_ReturnsCorrectCounts()
    {
        // Arrange
        float[] data = { -10, -5, 0, 5, 10 };
        int nBins = 2;
        // min = -10, max = 10, range = 20, binSize = 10
        // [-10, 0): -10, -5 -> 2
        // [0, 10]: 0, 5, 10 -> 3

        // Act
        var histogram = Histogram.FromData(data, nBins);

        // Assert
        histogram.Counts.ShouldBe(new[] { 2, 3 });
        histogram.BinStarts.ShouldBe(new[] { -10f, 0f });
    }

    [Test]
    public void FromData_NBinsZero_ThrowsException()
    {
        // Arrange
        float[] data = { 1, 2, 3 };
        int nBins = 0;

        // Act & Assert
        Should.Throw<IndexOutOfRangeException>(() => Histogram.FromData(data, nBins));
    }

    [Test]
    public void FromData_NBinsLargerThanData_ReturnsCorrectCounts()
    {
        // Arrange
        float[] data = { 0, 10 };
        int nBins = 10;
        // min = 0, max = 10, range = 10, binSize = 1
        // 0 -> index 0
        // 10 -> index 10 -> 9

        // Act
        var histogram = Histogram.FromData(data, nBins);

        // Assert
        var expectedCounts = new int[10];
        expectedCounts[0] = 1;
        expectedCounts[9] = 1;
        histogram.Counts.ShouldBe(expectedCounts);
    }
}
