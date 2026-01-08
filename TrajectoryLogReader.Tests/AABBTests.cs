using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class AABBTests
{
    [Test]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var aabb = new AABB(1, 2, 3, 4);
        aabb.MinX.ShouldBe(1);
        aabb.MinY.ShouldBe(2);
        aabb.MaxX.ShouldBe(3);
        aabb.MaxY.ShouldBe(4);
    }
}
