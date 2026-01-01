using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Fluence;
using System.Collections.Generic;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class IntersectionTests
{
    [Test]
    public void Intersect_SquareInsideRect_ReturnsSquare()
    {
        var rect = new Rect(0, 0, 10, 10);
        var polygon = new Polygon(new List<Point>
        {
            new(2, 2),
            new(8, 2),
            new(8, 8),
            new(2, 8)
        });

        var result = Intersection.Intersect(polygon, rect);

        result.Area().ShouldBe(36);
        result.Vertices.Count.ShouldBe(4);
    }

    [Test]
    public void Intersect_SquareOverlappingRect_ReturnsIntersection()
    {
        var rect = new Rect(0, 0, 10, 10);
        var polygon = new Polygon(new List<Point>
        {
            new(5, 5),
            new(15, 5),
            new(15, 15),
            new(5, 15)
        });

        var result = Intersection.Intersect(polygon, rect);

        // Intersection should be square from (5,5) to (10,10) -> 5x5 = 25
        result.Area().ShouldBe(25);
        result.Vertices.Count.ShouldBe(4);
    }

    [Test]
    public void Intersect_SquareOutsideRect_ReturnsEmpty()
    {
        var rect = new Rect(0, 0, 10, 10);
        var polygon = new Polygon(new List<Point>
        {
            new(12, 12),
            new(18, 12),
            new(18, 18),
            new(12, 18)
        });

        var result = Intersection.Intersect(polygon, rect);

        result.Area().ShouldBe(0);
        result.Vertices.Count.ShouldBe(0);
    }

    [Test]
    public void Intersect_RectInsideSquare_ReturnsRect()
    {
        var rect = new Rect(2, 2, 6, 6);
        var polygon = new Polygon(new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(10, 10),
            new(0, 10)
        });

        var result = Intersection.Intersect(polygon, rect);

        result.Area().ShouldBe(36);
        result.Vertices.Count.ShouldBe(4);
    }

    [Test]
    public void Intersect_TriangleClippedByRect_ReturnsClippedPolygon()
    {
        var rect = new Rect(0, 0, 10, 10);
        // Triangle with base at y=5, peak at (5, 15)
        var polygon = new Polygon(new List<Point>
        {
            new(0, 5),
            new(10, 5),
            new(5, 15)
        });

        var result = Intersection.Intersect(polygon, rect);

        // The triangle is clipped at y=10.
        // Base is at y=5, width 10.
        // At y=10, the width is 5 (halfway up the triangle).
        // The shape is a trapezoid with parallel sides 10 and 5, height 5.
        // Area = (10 + 5) / 2 * 5 = 37.5
        result.Area().ShouldBe(37.5);
    }

    [Test]
    public void Intersect_ConcavePolygonSplitByRect_ReturnsCorrectArea()
    {
        // U-shape polygon
        // (0,0) -> (10,0) -> (10,10) -> (8,10) -> (8,2) -> (2,2) -> (2,10) -> (0,10)
        var polygon = new Polygon(new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(10, 10),
            new(8, 10),
            new(8, 2),
            new(2, 2),
            new(2, 10),
            new(0, 10)
        });

        // Rect cutting through the legs of the U
        var rect = new Rect(0, 5, 10, 2); // y=5 to y=7

        var result = Intersection.Intersect(polygon, rect);

        // Intersection should be two rectangles:
        // Left leg: x=0..2, y=5..7 -> 2x2 = 4
        // Right leg: x=8..10, y=5..7 -> 2x2 = 4
        // Total area = 8
        result.Area().ShouldBe(8);
    }
}
