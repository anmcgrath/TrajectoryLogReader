using System.Numerics;
using System.Runtime.CompilerServices;

namespace TrajectoryLogReader.Fluence;

internal class RotatedRect
{
#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void GetRotatedRectAndBounds(
        Vector2 center, float width, float height, float cos, float sin,
        Span<Vector2> corners, out AABB bounds)
    {
        // 1. Calculate Half-Axes (Same as before)
        float hw = width * 0.5f;
        float hwX = hw * cos;
        float hwY = hw * sin;

        float hh = height * 0.5f;
        float hhX = -hh * sin;
        float hhY = hh * cos;

        // 2. Generate Corners (Same as before)
        // Top-Right
        corners[0] = new Vector2(center.X + hwX - hhX, center.Y + hwY - hhY);
        // Bottom-Right
        corners[1] = new Vector2(center.X + hwX + hhX, center.Y + hwY + hhY);
        // Bottom-Left
        corners[2] = new Vector2(center.X - hwX + hhX, center.Y - hwY + hhY);
        // Top-Left
        corners[3] = new Vector2(center.X - hwX - hhX, center.Y - hwY - hhY);

        float xExtent = Math.Abs(hwX) + Math.Abs(hhX);
        float yExtent = Math.Abs(hwY) + Math.Abs(hhY);

        bounds = new AABB(
            center.X - xExtent,
            center.Y - yExtent,
            center.X + xExtent,
            center.Y + yExtent
        );
    }
}