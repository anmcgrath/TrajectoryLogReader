using System.Numerics;
using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class ScanlineTests
{
    [Test]
    public void ProcessScanlines_SimpleRect_ProducesCorrectScanlines()
    {
        // A square from (2,2) to (4,4)
        // Vertices need to be ordered? The Scanline code handles arbitrary order?
        // "We only need the index, no sorting required for just 4 items."
        // But it expects convex polygon logic (left/right).
        // Let's provide them in order.
        var corners = new Point[]
        {
            new(2, 2), // Top-Left (actually min Y)
            new(4, 2), // Top-Right
            new(4, 4), // Bottom-Right
            new(2, 4) // Bottom-Left
        };

        var lines = new List<(int y, double startX, double endX)>();

        Scanline.ProcessScanlines(corners, 0, 10, (y, sx, ex) => { lines.Add((y, sx, ex)); });

        // Y range should be [2, 4]. (Wait, floor/ceil logic).
        // Code: int startY = (int)Math.Ceiling(minY);
        // minY = 2 -> startY = 2.
        // endY = Math.Floor(totalMaxY). MaxY = 4 -> endY = 4.
        // So lines at y=2, y=3, y=4.

        lines.Count.ShouldBe(3);

        // y=2
        lines[0].y.ShouldBe(2);
        lines[0].startX.ShouldBe(2f, 0.001f);
        lines[0].endX.ShouldBe(4f, 0.001f);

        // y=3
        lines[1].y.ShouldBe(3);
        lines[1].startX.ShouldBe(2f, 0.001f);
        lines[1].endX.ShouldBe(4f, 0.001f);

        // y=4
        lines[2].y.ShouldBe(4);
        lines[2].startX.ShouldBe(2f, 0.001f);
        lines[2].endX.ShouldBe(4f, 0.001f);
    }

    [Test]
    public void ProcessScanlines_Diamond_ProducesDiamondScanlines()
    {
        // Diamond centered at 5,5 radius 2.
        // Top: (5, 3)
        // Right: (7, 5)
        // Bottom: (5, 7)
        // Left: (3, 5)

        var corners = new Point[]
        {
            new(5, 3), // Min Y
            new(7, 5),
            new(5, 7),
            new(3, 5)
        };

        var lines = new List<(int y, double startX, double endX)>();
        Scanline.ProcessScanlines(corners, 0, 10, (y, sx, ex) => lines.Add((y, sx, ex)));

        // Y range: 3 to 7. (5 lines)
        // y=3: Tip (5, 3). Start=5, End=5.
        // y=4: Mid-way. Left side: (3,5)-(5,3). x goes from 5 to 3 in 2 steps. at y=4, x=4. Right: x=6.
        // y=5: Middle. x=3 to x=7.
        // y=6: x=4 to x=6.
        // y=7: Tip (5, 7). Start=5, End=5.

        lines.ShouldContain(l => l.y == 3 && Math.Abs(l.startX - 5) < 0.1 && Math.Abs(l.endX - 5) < 0.1);
        lines.ShouldContain(l => l.y == 4 && Math.Abs(l.startX - 4) < 0.1 && Math.Abs(l.endX - 6) < 0.1);
        lines.ShouldContain(l => l.y == 5 && Math.Abs(l.startX - 3) < 0.1 && Math.Abs(l.endX - 7) < 0.1);
        lines.ShouldContain(l => l.y == 6 && Math.Abs(l.startX - 4) < 0.1 && Math.Abs(l.endX - 6) < 0.1);
        lines.ShouldContain(l => l.y == 7 && Math.Abs(l.startX - 5) < 0.1 && Math.Abs(l.endX - 5) < 0.1);
    }

    [Test]
    public void ProcessScanlines_Clipped_RespectsClipY()
    {
        var corners = new Point[]
        {
            new(2, 2),
            new(4, 2),
            new(4, 8),
            new(2, 8)
        };

        // Clip Y to 4..6
        var lines = new List<(int y, double startX, double endX)>();
        Scanline.ProcessScanlines(corners, 4, 6, (y, sx, ex) => lines.Add((y, sx, ex)));

        lines.Count.ShouldBe(3); // 4, 5, 6
        lines[0].y.ShouldBe(4);
        lines[2].y.ShouldBe(6);
    }
}