using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Fluence;
using System.Collections.Generic;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class PolygonTests
{
    [Test]
    public void Area_Square_ReturnsCorrectArea()
    {
        var polygon = new Polygon(new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(10, 10),
            new(0, 10)
        });

        polygon.Area().ShouldBe(100);
    }

    [Test]
    public void Area_Triangle_ReturnsCorrectArea()
    {
        var polygon = new Polygon(new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(5, 10)
        });

        polygon.Area().ShouldBe(50);
    }

    [Test]
    public void Area_ConcavePolygon_ReturnsCorrectArea()
    {
        // L-shape
        var polygon = new Polygon(new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(10, 2),
            new(2, 2),
            new(2, 10),
            new(0, 10)
        });
        
        // Total 10x10 square (100) minus 8x8 square (64) = 36
        // Or 10*2 + 2*8 = 20 + 16 = 36
        polygon.Area().ShouldBe(36);
    }

    [Test]
    public void Area_WindingOrder_DoesNotAffectArea()
    {
        // Counter-clockwise
        var ccw = new Polygon(new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(10, 10),
            new(0, 10)
        });

        // Clockwise
        var cw = new Polygon(new List<Point>
        {
            new(0, 0),
            new(0, 10),
            new(10, 10),
            new(10, 0)
        });

        ccw.Area().ShouldBe(100);
        cw.Area().ShouldBe(100);
    }

    [Test]
    public void Area_EmptyPolygon_ReturnsZero()
    {
        var polygon = new Polygon(new List<Point>());
        polygon.Area().ShouldBe(0);
    }

    [Test]
    public void Area_TwoPoints_ReturnsZero()
    {
        var polygon = new Polygon(new List<Point>
        {
            new(0, 0),
            new(10, 10)
        });
        polygon.Area().ShouldBe(0);
    }
}
