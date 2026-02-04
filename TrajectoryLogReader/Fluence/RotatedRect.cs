using System.Numerics;
using System.Runtime.CompilerServices;

namespace TrajectoryLogReader.Fluence;

internal class RotatedRect
{
#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void GetRotatedRectAndBounds(
        Point2d center, double width, double height, double cos, double sin,
        Span<Point2d> corners, out AABB bounds)
    {
        // 1. Calculate Half-Axes (Same as before)
        var hw = width * 0.5;
        var hwX = hw * cos;
        var hwY = hw * sin;

        var hh = height * 0.5;
        var hhX = -hh * sin;
        var hhY = hh * cos;

        // 2. Generate Corners (Same as before)
        // Top-Right
        corners[0] = new Point2d(center.X + hwX - hhX, center.Y + hwY - hhY);
        // Bottom-Right
        corners[1] = new Point2d(center.X + hwX + hhX, center.Y + hwY + hhY);
        // Bottom-Left
        corners[2] = new Point2d(center.X - hwX + hhX, center.Y - hwY + hhY);
        // Top-Left
        corners[3] = new Point2d(center.X - hwX - hhX, center.Y - hwY - hhY);

        var xExtent = Math.Abs(hwX) + Math.Abs(hhX);
        var yExtent = Math.Abs(hwY) + Math.Abs(hhY);

        bounds = new AABB(
            center.X - xExtent,
            center.Y - yExtent,
            center.X + xExtent,
            center.Y + yExtent
        );
    }
}