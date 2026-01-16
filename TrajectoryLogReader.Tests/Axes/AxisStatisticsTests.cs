using System.Linq;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Axes;
using TrajectoryLogReader.LogStatistics;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Tests.Axes
{
    [TestFixture]
    public class AxisStatisticsTests
    {
        private TrajectoryLog _log;
        private const int NumSnapshots = 10;

        [SetUp]
        public void Setup()
        {
            _log = new TrajectoryLog();
            _log.Header = new Header
            {
                SamplingIntervalInMS = 20,
                NumberOfSnapshots = NumSnapshots,
                AxisScale = AxisScale.MachineScale,
                AxesSampled = new[] { Axis.GantryRtn, Axis.MLC },
                SamplesPerAxis = new[] { 1, 122 }
            };
            _log.Header.NumAxesSampled = 2;
            _log.AxisData = new AxisData[2];

            // 1. Gantry
            var gantryData = new AxisData(NumSnapshots, 2);
            for (int i = 0; i < NumSnapshots; i++)
            {
                gantryData.Data[i * 2] = i * 10; // Expected
                gantryData.Data[i * 2 + 1] = i * 10 + 2; // Actual, Error = 2
            }

            _log.AxisData[0] = gantryData;

            // 2. MLC
            var mlcSamplesPerSnapshot = 122 * 2;
            var mlcData = new AxisData(NumSnapshots, mlcSamplesPerSnapshot);
            // Leaf 0 Bank 0: Error 0.5
            for (int i = 0; i < NumSnapshots; i++)
            {
                int offset = i * mlcSamplesPerSnapshot + 4;
                mlcData.Data[offset] = 0; // Expected
                mlcData.Data[offset + 1] = 0.5f; // Actual
            }

            _log.AxisData[1] = mlcData;
        }

        [Test]
        public void Gantry_RootMeanSquareError_ReturnsCorrectValue()
        {
            // Error is constant 2.
            // RMS = Sqrt(Sum(2^2)/N) = Sqrt(4N/N) = 2.
            _log.Axes.Gantry.RootMeanSquareError().ShouldBe(2.0f, 0.001f);
        }

        [Test]
        public void Gantry_MaxError_ReturnsCorrectValue()
        {
            _log.Axes.Gantry.MaxError().ShouldBe(2.0f, 0.001f);
        }

        [Test]
        public void Gantry_ErrorHistogram_ReturnsCorrectCounts()
        {
            var hist = _log.Axes.Gantry.ErrorHistogram(1); // 1 bin
            hist.Counts[0].ShouldBe(NumSnapshots);
        }

        [Test]
        public void Mlc_RootMeanSquareError_ReturnsCorrectValue()
        {
            // Error is constant 0.5
            _log.Axes.Mlc.GetLeaf(Bank.A, 0)!.RootMeanSquareError().ShouldBe(0.5f, 0.001f);
        }

        [Test]
        public void Mlc_MaxError_ReturnsCorrectValue()
        {
            _log.Axes.Mlc.GetLeaf(Bank.A, 0).MaxError().ShouldBe(0.5f, 0.001f);
        }
    }
}