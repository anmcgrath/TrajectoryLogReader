using System.Numerics;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class IntersectionTests
{
    [Test]
    public void GetIntersectionArea_FullyInside_ReturnsPolyArea()
    {
        // Clip rect 0,0 to 10,10
        var clip = new AABB(0, 0, 10, 10);

        // Poly: Square 2,2 to 8,8 (6x6 = 36)
        var poly = new Point[]
        {
            new(2, 2),
            new(8, 2),
            new(8, 8),
            new(2, 8)
        };

        var area = Intersection.GetIntersectionArea(clip, poly);

        area.ShouldBe(36d, 0.001d);
    }

    [Test]
    public void GetIntersectionArea_FullyOutside_ReturnsZero()
    {
        var clip = new AABB(0, 0, 10, 10);

        // Poly: 12,12 to 14,14
        var poly = new Point[]
        {
            new(12, 12),
            new(14, 12),
            new(14, 14),
            new(12, 14)
        };

        var area = Intersection.GetIntersectionArea(clip, poly);

        area.ShouldBe(0d, 0.001d);
    }

    [Test]
    public void GetIntersectionArea_PartialOverlap_ReturnsIntersectionArea()
    {
        // Clip: 0,0 to 10,10
        var clip = new AABB(0, 0, 10, 10);

        // Poly: Square centered at 10,5 (overlap should be 5x5?? No.)
        // Let's define it explicitly.
        // 5,0 to 15,0 to 15,10 to 5,10
        // Overlap region is 5,0 to 10,0 to 10,10 to 5,10.
        // Width = 5, Height = 10. Area = 50.
        var poly = new Point[]
        {
            new(5, 0),
            new(15, 0),
            new(15, 10),
            new(5, 10)
        };

        var area = Intersection.GetIntersectionArea(clip, poly);

        area.ShouldBe(50d, 0.001d);
    }

    [Test]
    public void GetIntersectionArea_DiamondShape_ClippedCorners()
    {
        // Clip: 0,0 to 10,10
        var clip = new AABB(0, 0, 10, 10);

        // Poly: Rotated square (diamond) centered at 5,5
        // Tips at (5, 0) (10, 5) (5, 10) (0, 5) -> Exact fit inside. Area should be 0.5 * d1 * d2 = 0.5 * 10 * 10 = 50.
        var poly = new Point[]
        {
            new(5, 0),
            new(10, 5),
            new(5, 10),
            new(0, 5)
        };

        var area = Intersection.GetIntersectionArea(clip, poly);

        area.ShouldBe(50d, 0.001d);
    }

    [Test]
    public void GetIntersectionArea_DiamondShape_ExtendingOutside()
    {
        // Clip: 0,0 to 2,2
        var clip = new AABB(0, 0, 2, 2);

        var poly = new Point[]
        {
            new(1, -1),
            new(3, 1),
            new(1, 3),
            new(-1, 1)
        };

        var area = Intersection.GetIntersectionArea(clip, poly);

        area.ShouldBe(4d, 0.001d);
    }
}