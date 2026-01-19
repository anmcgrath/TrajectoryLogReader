using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Axes;

namespace TrajectoryLogReader.Tests.Axes
{
    [TestFixture]
    public class FilteredAxisTests
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
                AxesSampled = new[] { Axis.GantryRtn, Axis.MU },
                SamplesPerAxis = new[] { 1, 1 }
            };
            _log.Header.NumAxesSampled = 2;
            _log.AxisData = new AxisData[2];

            // 1. Gantry - constant error of 2
            var gantryData = new AxisData(NumSnapshots, 2);
            for (int i = 0; i < NumSnapshots; i++)
            {
                gantryData.Data[i * 2] = i * 10; // Expected
                gantryData.Data[i * 2 + 1] = i * 10 + 2; // Actual, Error = 2
            }

            _log.AxisData[0] = gantryData;

            // 2. MU - increasing from 0 to 90
            // DeltaMu will be: 0, 10, 10, 10, 10, 10, 10, 10, 10, 10
            var muData = new AxisData(NumSnapshots, 2);
            for (int i = 0; i < NumSnapshots; i++)
            {
                muData.Data[i * 2] = i * 10; // Expected MU
                muData.Data[i * 2 + 1] = i * 10; // Actual MU (no error)
            }

            _log.AxisData[1] = muData;
        }

        [Test]
        public void WithFilter_FiltersExpectedValues()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            // Filter to only include values where DeltaMu > 0
            // First DeltaMu is 0, rest are 10, so first value should be excluded
            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0);

            var expectedCount = filtered.ExpectedValues.Count();
            expectedCount.ShouldBe(NumSnapshots - 1); // First one excluded
        }

        [Test]
        public void WithFilter_FiltersActualValues()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0);

            var actualCount = filtered.ActualValues.Count();
            actualCount.ShouldBe(NumSnapshots - 1);
        }

        [Test]
        public void WithFilter_FiltersErrorValues()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0);

            var errorCount = filtered.ErrorValues.Count();
            errorCount.ShouldBe(NumSnapshots - 1);
        }

        [Test]
        public void WithFilter_RmsCalculatedOnFilteredData()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            // All gantry errors are 2, so RMS should still be 2 even after filtering
            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0);

            filtered.RootMeanSquareError().ShouldBe(2.0f, 0.001f);
        }

        [Test]
        public void WithFilter_MaxErrorCalculatedOnFilteredData()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0);

            filtered.MaxError().ShouldBe(2.0f, 0.001f);
        }

        [Test]
        public void WithFilter_HistogramCalculatedOnFilteredData()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0);
            var hist = filtered.ErrorHistogram(1);

            // Should have 9 values (first one filtered out)
            hist.Counts[0].ShouldBe(NumSnapshots - 1);
        }

        [Test]
        public void WithFilter_PreservesTimeInMs()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0);

            filtered.TimeInMs.ShouldBe(gantry.TimeInMs);
        }

        [Test]
        public void WithFilter_ValuesAreCached()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0);

            // Access values twice - should return same array instance
            var expected1 = filtered.ExpectedValues;
            var expected2 = filtered.ExpectedValues;

            ReferenceEquals(expected1, expected2).ShouldBeTrue();
        }

        [Test]
        public void WithFilter_FilterAllValues_ReturnsEmpty()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            // Filter with predicate that matches nothing
            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 1000);

            filtered.ExpectedValues.Count().ShouldBe(0);
            filtered.ActualValues.Count().ShouldBe(0);
            filtered.ErrorValues.Count().ShouldBe(0);
        }

        [Test]
        public void WithFilter_FilterNoValues_ReturnsAll()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            // Filter with predicate that matches everything
            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu >= 0);

            filtered.ExpectedValues.Count().ShouldBe(NumSnapshots);
        }

        [Test]
        public void WithFilter_CanChainFilters()
        {
            var gantry = _log.Axes.Gantry;
            var deltaMu = _log.Axes.DeltaMu;

            // Chain two filters
            var filtered = gantry
                .WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0)
                .WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu < 20);

            // DeltaMu values: 0, 10, 10, 10, 10, 10, 10, 10, 10, 10
            // First filter removes first (0), second keeps all remaining (all are 10)
            filtered.ExpectedValues.Count().ShouldBe(NumSnapshots - 1);
        }

        [Test]
        public void WithFilter_WithVaryingErrors_RmsCalculatedCorrectly()
        {
            // Create a log with varying errors
            var log = new TrajectoryLog();
            log.Header = new Header
            {
                SamplingIntervalInMS = 20,
                NumberOfSnapshots = 4,
                AxisScale = AxisScale.MachineScale,
                AxesSampled = new[] { Axis.GantryRtn, Axis.MU },
                SamplesPerAxis = new[] { 1, 1 }
            };
            log.Header.NumAxesSampled = 2;
            log.AxisData = new AxisData[2];

            // Gantry with errors: 0, 1, 2, 3
            var gantryData = new AxisData(4, 2);
            gantryData.Data[0] = 0;
            gantryData.Data[1] = 0; // Error 0
            gantryData.Data[2] = 0;
            gantryData.Data[3] = 1; // Error 1
            gantryData.Data[4] = 0;
            gantryData.Data[5] = 2; // Error 2
            gantryData.Data[6] = 0;
            gantryData.Data[7] = 3; // Error 3
            log.AxisData[0] = gantryData;

            // MU: 0, 10, 20, 30 (DeltaMu: 0, 10, 10, 10)
            var muData = new AxisData(4, 2);
            muData.Data[0] = 0;
            muData.Data[1] = 0;
            muData.Data[2] = 10;
            muData.Data[3] = 10;
            muData.Data[4] = 20;
            muData.Data[5] = 20;
            muData.Data[6] = 30;
            muData.Data[7] = 30;
            log.AxisData[1] = muData;

            var gantry = log.Axes.Gantry;
            var deltaMu = log.Axes.DeltaMu;

            // Filter to exclude first value (DeltaMu = 0)
            // Remaining errors: 1, 2, 3
            // RMS = sqrt((1 + 4 + 9) / 3) = sqrt(14/3) = sqrt(4.667) ≈ 2.16
            var filtered = gantry.WithFilter(deltaMu, RecordType.ExpectedPosition, mu => mu > 0);

            var expectedRms = (float)Math.Sqrt((1 + 4 + 9) / 3.0);
            filtered.RootMeanSquareError().ShouldBe(expectedRms, 0.001f);
        }
    }
}