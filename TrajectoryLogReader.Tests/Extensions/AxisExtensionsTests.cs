using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Extensions;
using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Tests.Extensions
{
    [TestFixture]
    public class AxisExtensionsTests
    {
        [Test]
        public void GetUnit_RotationalAxis_ReturnsDegrees()
        {
            Axis.GantryRtn.GetUnit().ShouldBe(Unit.Degrees);
            Axis.CollRtn.GetUnit().ShouldBe(Unit.Degrees);
            Axis.CouchRtn.GetUnit().ShouldBe(Unit.Degrees);
        }

        [Test]
        public void GetUnit_LinearAxis_ReturnsCentimeters()
        {
            Axis.Y1.GetUnit().ShouldBe(Unit.Centimeters);
            Axis.X1.GetUnit().ShouldBe(Unit.Centimeters);
            Axis.CouchVrt.GetUnit().ShouldBe(Unit.Centimeters);
            Axis.MLC.GetUnit().ShouldBe(Unit.Centimeters);
        }

        [Test]
        public void GetUnit_MU_ReturnsMU()
        {
            Axis.MU.GetUnit().ShouldBe(Unit.MU);
        }

        [Test]
        public void GetUnit_Dimensionless_ReturnsDimensionless()
        {
            Axis.ControlPoint.GetUnit().ShouldBe(Unit.Dimensionless);
            Axis.BeamHold.GetUnit().ShouldBe(Unit.Dimensionless);
        }
    }
}