using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Axes;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Tests.Axes
{
    [TestFixture]
    public class LogAxesTests
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
                AxisScale = AxisScale.MachineScale, // Use MachineScale for simplicity
                AxesSampled = new[] { Axis.GantryRtn, Axis.Y1, Axis.MLC },
                MlcModel = MLCModel.NDS120,
                SamplesPerAxis = new[] { 1, 1, 122 }
            };
            _log.Header.NumAxesSampled = _log.Header.AxesSampled.Length;
            _log.AxisData = new AxisData[_log.Header.NumAxesSampled];

            // 1. Gantry
            var gantryData = new AxisData(NumSnapshots, 2);
            for (int i = 0; i < NumSnapshots; i++)
            {
                gantryData.Data[i * 2] = i * 10; // Expected: 0, 10, 20...
                gantryData.Data[i * 2 + 1] = i * 10 + 1; // Actual: 1, 11, 21...
            }
            _log.AxisData[0] = gantryData;

            // 2. Y1
            var y1Data = new AxisData(NumSnapshots, 2);
             for (int i = 0; i < NumSnapshots; i++)
            {
                y1Data.Data[i * 2] = 5;
                y1Data.Data[i * 2 + 1] = 5.5f;
            }
            _log.AxisData[1] = y1Data;

            // 3. MLC
            var mlcSamplesPerSnapshot = 122 * 2;
            var mlcData = new AxisData(NumSnapshots, mlcSamplesPerSnapshot);
            // Moving leaf: Bank 0, Leaf 0.
            // Static leaf: Bank 0, Leaf 1.
            for (int i = 0; i < NumSnapshots; i++)
            {
                // Bank 0, Leaf 0 (Index 4 + 0)
                int offsetL0 = i * mlcSamplesPerSnapshot + 4;
                mlcData.Data[offsetL0] = i * 0.1f; // Expected moving
                mlcData.Data[offsetL0 + 1] = i * 0.1f + 0.01f; // Actual
                
                // Bank 0, Leaf 1 (Index 4 + 2)
                int offsetL1 = i * mlcSamplesPerSnapshot + 4 + 2;
                mlcData.Data[offsetL1] = 1.0f; // Expected static
                mlcData.Data[offsetL1 + 1] = 1.0f; // Actual static
            }
            _log.AxisData[2] = mlcData;
        }

        [Test]
        public void Gantry_Expected_ReturnsCorrectValues()
        {
            var expected = _log.Axes.Gantry.Expected().ToList();
            expected.Count.ShouldBe(NumSnapshots);
            expected[0].ShouldBe(0);
            expected[1].ShouldBe(10);
            expected[9].ShouldBe(90);
        }

        [Test]
        public void Gantry_Actual_ReturnsCorrectValues()
        {
            var actual = _log.Axes.Gantry.Actual().ToList();
            actual.Count.ShouldBe(NumSnapshots);
            actual[0].ShouldBe(1);
            actual[1].ShouldBe(11);
        }

        [Test]
        public void Gantry_Deltas_ReturnsCorrectValues()
        {
            var deltas = _log.Axes.Gantry.Deltas().ToList();
            deltas.Count.ShouldBe(NumSnapshots);
            // Actual - Expected = 1
            deltas.All(d => Math.Abs(d - 1) < 0.001f).ShouldBeTrue();
        }

        [Test]
        public void Gantry_WithScale_ConvertsValues()
        {
            // MachineScale is 0..360 usually.
            // IEC: 180 is 0 IEC. 
            // If MachineScale 0 -> IEC 180.
            // Log is in MachineScale. 
            // Request IEC.
            
            var expectedIEC = _log.Axes.Gantry.WithScale(AxisScale.ModifiedIEC61217).Expected().ToList();
            // Machine 0 -> IEC 180
            expectedIEC[0].ShouldBe(180); 
            // Machine 10 -> IEC 170 (Counter clockwise?)
            expectedIEC[0].ShouldNotBe(0);
        }

        [Test]
        public void Mlc_SpecificLeaf_ReturnsCorrectValues()
        {
            // Bank 0 (B), Leaf 0
            var leaf = _log.Axes.Mlc[Bank.B, 0];
            leaf.ShouldNotBeNull();
            var exp = leaf.Expected().ToList();
            exp[0].ShouldBe(0f); // 0 * 0.1
            exp[1].ShouldBe(0.1f); // 1 * 0.1
        }

        [Test]
        public void MovingMLCs_ReturnsOnlyMovingLeaves()
        {
            var moving = _log.Axes.MovingMlc.ToList();
            // We set Leaf 0 Bank 0 to move. Leaf 1 Bank 0 is static.
            // Others are 0 (static).
            // So only 1 leaf should be in moving list?
            
            moving.Count.ShouldBe(1);
            moving[0].LeafIndex.ShouldBe(0);
            moving[0].Bank.ShouldBe(Bank.B);
        }

        [Test]
        public void MovingMLCs_WithHighThreshold_ReturnsEmpty()
        {
            // Leaf 0 moves by 0.1 per snapshot (total 1.0).
            // If threshold is 2.0, it shouldn't be moving.
            
            var moving = _log.Axes.GetMovingMlc(2.0f).ToList();
            moving.Count.ShouldBe(0);
        }

        [Test]
        public void Gantry_Deltas_HandlesWrapAround()
        {
            // Update Gantry data to have wrap around
            var gantryData = _log.GetAxisData(Axis.GantryRtn);
            // Exp: 359, Act: 1 (Machine Scale). 
            // In Machine Scale (CW), 359 -> 0 -> 1 is +2 degrees.
            
            gantryData.Data[0] = 359;
            gantryData.Data[1] = 1;
            
            var delta = _log.Axes.Gantry.Deltas().First();
            delta.ShouldBe(2);
        }

        [Test]
        public void Couch_Deltas_HandlesOffsetWrap()
        {
            // Case: Expected 100, Actual 0.1. 
            // Diff = 0.1 - 100 = -99.9.
            // With wrap 100, -99.9 + 100 = 0.1.
            
            var couchData = _log.GetAxisData(Axis.Y1); // Y1 is index 1, but we need a Couch Axis.
            // Setup uses [Gantry, Y1, MLC]. 
            // I need to add a Couch Axis to setup or mock it.
            // I'll re-initialize the log for this test locally or just modify Setup?
            // Easier to modify Setup to include CouchLat.
            
            var log = new TrajectoryLog();
            log.Header = new Header
            {
                SamplingIntervalInMS = 20,
                NumberOfSnapshots = 1,
                AxisScale = AxisScale.MachineScale,
                AxesSampled = new[] { Axis.CouchLat },
                SamplesPerAxis = new[] { 1 }
            };
            log.Header.NumAxesSampled = 1;
            log.AxisData = new AxisData[1];
            log.AxisData[0] = new AxisData(1, 2);
            log.AxisData[0].Data[0] = 100.0f; // Exp
            log.AxisData[0].Data[1] = 0.1f;   // Act
            
            var delta = log.Axes.CouchLat.Deltas().First();
            delta.ShouldBe(0.1f, 0.001f);
        }
        [Test]
        public void GetAxis_ReturnsCorrectAccessor()
        {
            var accessor = _log.Axes.GetAxis(Axis.GantryRtn);
            accessor.ShouldNotBeNull();
            accessor.Expected().Count().ShouldBe(NumSnapshots);
        }

        [Test]
        public void GetAxis_ThrowsForMLC()
        {
            Should.Throw<ArgumentException>(() => _log.Axes.GetAxis(Axis.MLC));
        }
    }
}