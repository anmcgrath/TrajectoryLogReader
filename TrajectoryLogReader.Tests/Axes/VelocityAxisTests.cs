using System.Linq;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Tests.Axes
{
    [TestFixture]
    public class VelocityAxisTests
    {
        private TrajectoryLog _log;
        private const int NumSnapshots = 3;
        private const int SamplingInterval = 20; // 20ms = 0.02s

        [SetUp]
        public void Setup()
        {
            _log = new TrajectoryLog();
            _log.Header = new Header
            {
                SamplingIntervalInMS = SamplingInterval,
                NumberOfSnapshots = NumSnapshots,
                AxisScale = AxisScale.MachineScale,
                AxesSampled = new[] { Axis.MLC },
                SamplesPerAxis = new[] { 122 },
                MlcModel = MLCModel.NDS120
            };
            _log.Header.NumAxesSampled = 1;
            _log.AxisData = new AxisData[1];

            var mlcData = new AxisData(NumSnapshots, 122 * 2);
            // Leaf 0, Bank 0 (index 4)
            // t0: 0.0
            // t1: 0.1 (Speed = 0.1 / 0.02 = 5 cm/s)
            // t2: 0.2 (Speed = 0.1 / 0.02 = 5 cm/s)

            // Snapshot 0
            mlcData.Data[4] = 0.0f; // Exp
            mlcData.Data[5] = 0.0f; // Act

            // Snapshot 1
            mlcData.Data[122 * 2 + 4] = 0.1f; // Exp
            mlcData.Data[122 * 2 + 5] = 0.1f; // Act

            // Snapshot 2
            mlcData.Data[122 * 2 * 2 + 4] = 0.2f; // Exp
            mlcData.Data[122 * 2 * 2 + 5] = 0.2f; // Act

            _log.AxisData[0] = mlcData;
        }

        [Test]
        public void Leaf_Velocity_CalculatesCorrectSpeed()
        {
            var leaf = _log.Axes.Mlc.GetLeaf(Bank.B, 0);
            var velocity = leaf.GetVelocity().Expected.ToList();

            velocity.Count.ShouldBe(3);
            velocity[0].ShouldBe(0f); // First point is 0
            velocity[1].ShouldBe(5.0f, 0.001f);
            velocity[2].ShouldBe(5.0f, 0.001f);
        }

        [Test]
        public void MlcVelocity_AggregatesCorrectly_NoError()
        {
            var maxVel = _log.Axes.Mlc.Velocity.MaxError();
            maxVel.ShouldBe(0f, 0.001f);
        }

        [Test]
        public void MlcVelocity_AggregatesCorrectly_WithError()
        {
            // Introduce error BEFORE accessing the property
            var mlcData = _log.AxisData[0];
            mlcData.Data[122 * 2 * 2 + 5] = 0.21f;

            var maxError = _log.Axes.Mlc.Velocity.MaxError();
            maxError.ShouldBe(0.5f, 0.001f);
        }
    }
}