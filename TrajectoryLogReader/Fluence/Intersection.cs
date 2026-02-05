using System.Numerics;
using System.Runtime.CompilerServices;

namespace TrajectoryLogReader.Fluence;

internal class Intersection
{
    /// <summary>
    /// Calculates the intersection area between an Axis-Aligned Rectangle (clipRect) 
    /// and a Rotated Rectangle (defined by 4 vertices).
    /// </summary>
    /// <param name="clipRect">The stationary axis-aligned rectangle.</param>
    /// <param name="subjectPoly">The 4 vertices of the rotated rectangle.</param>
    /// <returns>The area of intersection.</returns>
#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
    public static double GetIntersectionArea(AABB clipRect, ReadOnlySpan<Point> subjectPoly)
    {
        // 1. Prepare buffers on the stack to avoid GC.
        // A rect-rect intersection produces at most 8 vertices.
        const int maxVertices = 16;

        // Double buffering: 'input' is the geometry to clip, 'output' is the result of a clip stage.
        Span<Point> buffer1 = stackalloc Point[maxVertices];
        Span<Point> buffer2 = stackalloc Point[maxVertices];

        // Copy initial rotated rect into buffer1
        if (subjectPoly.Length > maxVertices) return 0; // Should not happen for a rectangle
        subjectPoly.CopyTo(buffer1);
        int inputCount = subjectPoly.Length;

        // 2. Run the Sutherland-Hodgman pipeline (4 stages for AABB)

        // --- Stage 1: Clip against MinX (Left) ---
        int outputCount = 0;
        if (inputCount > 0)
        {
            var prev = buffer1[inputCount - 1];
            for (int i = 0; i < inputCount; i++)
            {
                var curr = buffer1[i];
                // "Inside" is x >= MinX
                bool isPrevIn = prev.X >= clipRect.MinX;
                bool isCurrIn = curr.X >= clipRect.MinX;

                if (isCurrIn)
                {
                    if (!isPrevIn) // Out -> In: Add Intersection
                    {
                        var t = (clipRect.MinX - prev.X) / (curr.X - prev.X);
                        buffer2[outputCount++] = new Point(clipRect.MinX, prev.Y + t * (curr.Y - prev.Y));
                    }

                    buffer2[outputCount++] = curr; // In -> In: Add Current
                }
                else if (isPrevIn) // In -> Out: Add Intersection
                {
                    var t = (clipRect.MinX - prev.X) / (curr.X - prev.X);
                    buffer2[outputCount++] = new Point(clipRect.MinX, prev.Y + t * (curr.Y - prev.Y));
                }

                prev = curr;
            }
        }

        // Swap buffers: buffer2 becomes input for next stage
        inputCount = outputCount;
        if (inputCount == 0) return 0f;

        // --- Stage 2: Clip against MaxX (Right) ---
        outputCount = 0;
        {
            var prev = buffer2[inputCount - 1];
            for (int i = 0; i < inputCount; i++)
            {
                var curr = buffer2[i];
                bool isPrevIn = prev.X <= clipRect.MaxX;
                bool isCurrIn = curr.X <= clipRect.MaxX;

                if (isCurrIn)
                {
                    if (!isPrevIn)
                    {
                        var t = (clipRect.MaxX - prev.X) / (curr.X - prev.X);
                        buffer1[outputCount++] = new Point(clipRect.MaxX, prev.Y + t * (curr.Y - prev.Y));
                    }

                    buffer1[outputCount++] = curr;
                }
                else if (isPrevIn)
                {
                    var t = (clipRect.MaxX - prev.X) / (curr.X - prev.X);
                    buffer1[outputCount++] = new Point(clipRect.MaxX, prev.Y + t * (curr.Y - prev.Y));
                }

                prev = curr;
            }
        }

        inputCount = outputCount;
        if (inputCount == 0) return 0f;

        // --- Stage 3: Clip against MinY (Bottom) ---
        outputCount = 0;
        {
            var prev = buffer1[inputCount - 1];
            for (int i = 0; i < inputCount; i++)
            {
                var curr = buffer1[i];
                bool isPrevIn = prev.Y >= clipRect.MinY;
                bool isCurrIn = curr.Y >= clipRect.MinY;

                if (isCurrIn)
                {
                    if (!isPrevIn)
                    {
                        var t = (clipRect.MinY - prev.Y) / (curr.Y - prev.Y);
                        buffer2[outputCount++] = new Point(prev.X + t * (curr.X - prev.X), clipRect.MinY);
                    }

                    buffer2[outputCount++] = curr;
                }
                else if (isPrevIn)
                {
                    var t = (clipRect.MinY - prev.Y) / (curr.Y - prev.Y);
                    buffer2[outputCount++] = new Point(prev.X + t * (curr.X - prev.X), clipRect.MinY);
                }

                prev = curr;
            }
        }

        inputCount = outputCount;
        if (inputCount == 0) return 0;

        // --- Stage 4: Clip against MaxY (Top) ---
        outputCount = 0;
        {
            var prev = buffer2[inputCount - 1];
            for (int i = 0; i < inputCount; i++)
            {
                var curr = buffer2[i];
                bool isPrevIn = prev.Y <= clipRect.MaxY;
                bool isCurrIn = curr.Y <= clipRect.MaxY;

                if (isCurrIn)
                {
                    if (!isPrevIn)
                    {
                        var t = (clipRect.MaxY - prev.Y) / (curr.Y - prev.Y);
                        buffer1[outputCount++] = new Point(prev.X + t * (curr.X - prev.X), clipRect.MaxY);
                    }

                    buffer1[outputCount++] = curr;
                }
                else if (isPrevIn)
                {
                    var t = (clipRect.MaxY - prev.Y) / (curr.Y - prev.Y);
                    buffer1[outputCount++] = new Point(prev.X + t * (curr.X - prev.X), clipRect.MaxY);
                }

                prev = curr;
            }
        }

        inputCount = outputCount;
        if (inputCount < 3) return 0;

        // 3. Calculate Area (Shoelace Formula)
        // Area = 0.5 * |sum(x_i * y_{i+1} - x_{i+1} * y_i)|
        var area = 0d;
        var last = buffer1[inputCount - 1];
        for (int i = 0; i < inputCount; i++)
        {
            var curr = buffer1[i];
            area += (last.X * curr.Y) - (last.Y * curr.X);
            last = curr;
        }

        return Math.Abs(area) * 0.5f;
    }

    /// <summary>
    /// Optimized intersection area calculation for a unit pixel at integer coordinates.
    /// Avoids AABB struct creation and uses inline constants.
    /// </summary>
    /// <param name="col">Column index (pixel spans [col, col+1] in X).</param>
    /// <param name="row">Row index (pixel spans [row, row+1] in Y).</param>
    /// <param name="subjectPoly">The 4 vertices of the rotated rectangle.</param>
    /// <returns>The area of intersection (0 to 1).</returns>
#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static double GetIntersectionAreaPixel(int col, int row, ReadOnlySpan<Point> subjectPoly)
    {
        // Pixel bounds
        double minX = col;
        double maxX = col + 1;
        double minY = row;
        double maxY = row + 1;

        // 1. Prepare buffers on the stack
        const int maxVertices = 16;
        Span<Point> buffer1 = stackalloc Point[maxVertices];
        Span<Point> buffer2 = stackalloc Point[maxVertices];

        // Copy initial polygon
        subjectPoly.CopyTo(buffer1);
        int inputCount = subjectPoly.Length;

        // 2. Sutherland-Hodgman clipping (4 stages)

        // --- Stage 1: Clip against MinX (Left) ---
        int outputCount = 0;
        {
            var prev = buffer1[inputCount - 1];
            for (int i = 0; i < inputCount; i++)
            {
                var curr = buffer1[i];
                bool isPrevIn = prev.X >= minX;
                bool isCurrIn = curr.X >= minX;

                if (isCurrIn)
                {
                    if (!isPrevIn)
                    {
                        var invDx = 1d / (curr.X - prev.X);
                        var t = (minX - prev.X) * invDx;
                        buffer2[outputCount++] = new Point(minX, prev.Y + t * (curr.Y - prev.Y));
                    }

                    buffer2[outputCount++] = curr;
                }
                else if (isPrevIn)
                {
                    var invDx = 1d / (curr.X - prev.X);
                    var t = (minX - prev.X) * invDx;
                    buffer2[outputCount++] = new Point(minX, prev.Y + t * (curr.Y - prev.Y));
                }

                prev = curr;
            }
        }

        inputCount = outputCount;
        if (inputCount == 0) return 0;

        // --- Stage 2: Clip against MaxX (Right) ---
        outputCount = 0;
        {
            var prev = buffer2[inputCount - 1];
            for (int i = 0; i < inputCount; i++)
            {
                var curr = buffer2[i];
                bool isPrevIn = prev.X <= maxX;
                bool isCurrIn = curr.X <= maxX;

                if (isCurrIn)
                {
                    if (!isPrevIn)
                    {
                        var invDx = 1d / (curr.X - prev.X);
                        var t = (maxX - prev.X) * invDx;
                        buffer1[outputCount++] = new Point(maxX, prev.Y + t * (curr.Y - prev.Y));
                    }

                    buffer1[outputCount++] = curr;
                }
                else if (isPrevIn)
                {
                    var invDx = 1d / (curr.X - prev.X);
                    var t = (maxX - prev.X) * invDx;
                    buffer1[outputCount++] = new Point(maxX, prev.Y + t * (curr.Y - prev.Y));
                }

                prev = curr;
            }
        }

        inputCount = outputCount;
        if (inputCount == 0) return 0;

        // --- Stage 3: Clip against MinY (Bottom) ---
        outputCount = 0;
        {
            var prev = buffer1[inputCount - 1];
            for (int i = 0; i < inputCount; i++)
            {
                var curr = buffer1[i];
                bool isPrevIn = prev.Y >= minY;
                bool isCurrIn = curr.Y >= minY;

                if (isCurrIn)
                {
                    if (!isPrevIn)
                    {
                        var invDy = 1d / (curr.Y - prev.Y);
                        var t = (minY - prev.Y) * invDy;
                        buffer2[outputCount++] = new Point(prev.X + t * (curr.X - prev.X), minY);
                    }

                    buffer2[outputCount++] = curr;
                }
                else if (isPrevIn)
                {
                    var invDy = 1d / (curr.Y - prev.Y);
                    var t = (minY - prev.Y) * invDy;
                    buffer2[outputCount++] = new Point(prev.X + t * (curr.X - prev.X), minY);
                }

                prev = curr;
            }
        }

        inputCount = outputCount;
        if (inputCount == 0) return 0;

        // --- Stage 4: Clip against MaxY (Top) ---
        outputCount = 0;
        {
            var prev = buffer2[inputCount - 1];
            for (int i = 0; i < inputCount; i++)
            {
                var curr = buffer2[i];
                bool isPrevIn = prev.Y <= maxY;
                bool isCurrIn = curr.Y <= maxY;

                if (isCurrIn)
                {
                    if (!isPrevIn)
                    {
                        var invDy = 1d / (curr.Y - prev.Y);
                        var t = (maxY - prev.Y) * invDy;
                        buffer1[outputCount++] = new Point(prev.X + t * (curr.X - prev.X), maxY);
                    }

                    buffer1[outputCount++] = curr;
                }
                else if (isPrevIn)
                {
                    var invDy = 1d / (curr.Y - prev.Y);
                    var t = (maxY - prev.Y) * invDy;
                    buffer1[outputCount++] = new Point(prev.X + t * (curr.X - prev.X), maxY);
                }

                prev = curr;
            }
        }

        inputCount = outputCount;
        if (inputCount < 3) return 0;

        // 3. Calculate Area (Shoelace Formula)
        var area = 0d;
        var last = buffer1[inputCount - 1];
        for (int i = 0; i < inputCount; i++)
        {
            var curr = buffer1[i];
            area += (last.X * curr.Y) - (last.Y * curr.X);
            last = curr;
        }

        return Math.Abs(area) * 0.5;
    }
}