using System.Linq;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Axes;

namespace TrajectoryLogReader.Tests.Axes
{
    [TestFixture]
    public class CombinedAxisTests
    {
        [Test]
        public void JawsX_CalculatesFieldSizeCorrectly()
        {
            // Setup log with X1 and X2
            // X1 in Machine: 10cm. IEC: -10cm.
            // X2 in Machine: 10cm. IEC: +10cm.
            // JawsX = X2_IEC - X1_IEC = 10 - (-10) = 20.
            
            var log = new TrajectoryLog();
            log.Header = new Header
            {
                SamplingIntervalInMS = 20,
                NumberOfSnapshots = 1,
                AxisScale = AxisScale.MachineScale,
                AxesSampled = new[] { Axis.X1, Axis.X2 },
                SamplesPerAxis = new[] { 1, 1 }
            };
            log.Header.NumAxesSampled = 2;
            log.AxisData = new AxisData[2];
            
            // X1
            log.AxisData[0] = new AxisData(1, 2);
            log.AxisData[0].Data[0] = 10.0f; // Exp
            log.AxisData[0].Data[1] = 10.0f; // Act
            
            // X2
            log.AxisData[1] = new AxisData(1, 2);
            log.AxisData[1].Data[0] = 10.0f; // Exp
            log.AxisData[1].Data[1] = 10.0f; // Act
            
            var jawsX = log.Axes.JawsX;
            var val = jawsX.Expected.First();
            
            // X1 (Machine 10) -> IEC -10.
            // X2 (Machine 10) -> IEC 10.
            // X2 - X1 = 20.
            val.ShouldBe(20.0f, 0.001f);
        }
    }
}