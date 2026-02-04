using System.Numerics;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class FastRectTests
{
    [Test]
    public void GetRotatedRectAndBounds_NoRotation_ReturnsCorrectCornersAndBounds()
    {
        var center = new Point2d(10, 10);
        double width = 4;
        double height = 2;
        double cos = 1; // 0 degrees
        double sin = 0;
        
        Span<Point2d> corners = stackalloc Point2d[4];
        RotatedRect.GetRotatedRectAndBounds(center, width, height, cos, sin, corners, out var bounds);
        
        // Expected corners:
        // TR: 10 + 2, 10 + 1 = 12, 11
        // BR: 10 + 2, 10 - 1 ?? Wait, the implementation uses Y-up logic or Y-down?
        // Let's check the code:
        /*
         // Top-Right
        corners[0] = new Vector2(center.X + hwX - hhX, center.Y + hwY - hhY);
        hwX = hw * 1 = 2
        hwY = 0
        hhX = 0
        hhY = hh * 1 = 1
        
        TR = (10 + 2 - 0, 10 + 0 - 1) = (12, 9) ... Wait.
        The code says:
        corners[0] = new Vector2(center.X + hwX - hhX, center.Y + hwY - hhY);
        center.Y + hwY - hhY -> 10 + 0 - 1 = 9.
        
        So "Top" implies lower Y? Or is Y up?
        Usually "Top" means higher Y in math, lower Y in screens.
        But let's just verify the values.
        
        hw = 2, hh = 1.
        
        TR: (12, 9)
        BR: (10 + 2 + 0, 10 + 0 + 1) = (12, 11)
        BL: (10 - 2 + 0, 10 - 0 + 1) = (8, 11)
        TL: (10 - 2 - 0, 10 - 0 - 1) = (8, 9)
        */

        corners[0].ShouldBe(new Point2d(12, 9));
        corners[1].ShouldBe(new Point2d(12, 11));
        corners[2].ShouldBe(new Point2d(8, 11));
        corners[3].ShouldBe(new Point2d(8, 9));
        
        // Bounds should encompass all
        bounds.MinX.ShouldBe(8);
        bounds.MaxX.ShouldBe(12);
        bounds.MinY.ShouldBe(9);
        bounds.MaxY.ShouldBe(11);
    }
    
    [Test]
    public void GetRotatedRectAndBounds_90DegRotation_ReturnsSwappedDims()
    {
        var center = new Point2d(0, 0);
        double width = 4; // hw = 2
        double height = 2; // hh = 1
        // 90 deg: cos=0, sin=1
        double cos = 0;
        double sin = 1;
        
        Span<Point2d> corners = stackalloc Point2d[4];
        RotatedRect.GetRotatedRectAndBounds(center, width, height, cos, sin, corners, out var bounds);
        
        // hwX = 2 * 0 = 0
        // hwY = 2 * 1 = 2
        // hhX = -1 * 1 = -1
        // hhY = 1 * 0 = 0
        
        // TR: (0 + 0 - (-1), 0 + 2 - 0) = (1, 2)
        // BR: (0 + 0 + (-1), 0 + 2 + 0) = (-1, 2)
        // BL: (0 - 0 + (-1), 0 - 2 + 0) = (-1, -2)
        // TL: (0 - 0 - (-1), 0 - 2 - 0) = (1, -2)
        
        corners[0].ShouldBe(new Point2d(1, 2));
        corners[1].ShouldBe(new Point2d(-1, 2));
        corners[2].ShouldBe(new Point2d(-1, -2));
        corners[3].ShouldBe(new Point2d(1, -2));
        
        bounds.MinX.ShouldBe(-1);
        bounds.MaxX.ShouldBe(1);
        bounds.MinY.ShouldBe(-2);
        bounds.MaxY.ShouldBe(2);
    }
}
