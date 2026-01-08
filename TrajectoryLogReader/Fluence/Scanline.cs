using System.Numerics;

namespace TrajectoryLogReader.Fluence;

public class Scanline
{
    public static void ProcessScanlines(
        ReadOnlySpan<Vector2> corners, // The 4 corners from previous step
        int clipMinY, // Top of the clipping viewport/grid
        int clipMaxY, // Bottom of the clipping viewport/grid
        Action<int, float, float> onScanline) // Callback: (y, startX, endX)
    {
        // 1. Find the Top Vertex (Min Y)
        // We only need the index, no sorting required for just 4 items.
        int topIdx = 0;
        float minY = corners[0].Y;
        for (int i = 1; i < 4; i++)
        {
            if (corners[i].Y < minY)
            {
                minY = corners[i].Y;
                topIdx = i;
            }
        }

        // 2. Identify "Left" and "Right" indices
        // We trace indices cyclically. 
        // One neighbor is "Next" (Left-ish?), one is "Prev" (Right-ish?)
        // We determine which is strictly Left/Right by checking X or just slope.
        // A robust way for Convex polygons:
        // The "Left" side is the one where Y increases? Both increase.
        // We just initialize two walkers.

        // Previous vertex in list
        int leftIdx = (topIdx - 1 + 4) % 4;
        // Next vertex in list
        int rightIdx = (topIdx + 1) % 4;

        // Determine strictly which is left/right based on X
        // (Handle the case where one might be directly below the top)
        if (corners[rightIdx].X < corners[leftIdx].X)
        {
            // Swap to ensure 'leftIdx' is actually on the left
            (leftIdx, rightIdx) = (rightIdx, leftIdx);
        }

        // 3. Initialize Walkers
        // Left Side
        Vector2 vLeftTop = corners[topIdx];
        Vector2 vLeftBot = corners[leftIdx];
        float leftSlope = (vLeftBot.X - vLeftTop.X) / (vLeftBot.Y - vLeftTop.Y);
        float currentLeftX = vLeftTop.X;

        // Right Side
        Vector2 vRightTop = corners[topIdx];
        Vector2 vRightBot = corners[rightIdx];
        float rightSlope = (vRightBot.X - vRightTop.X) / (vRightBot.Y - vRightTop.Y);
        float currentRightX = vRightTop.X;

        // 4. Loop Logic
        int startY = (int)Math.Ceiling(minY);
        // Pre-advance X to the center of the first scanline if needed, 
        // or just interpolate from the exact Y.
        currentLeftX += (startY - vLeftTop.Y) * leftSlope;
        currentRightX += (startY - vRightTop.Y) * rightSlope;

        // Track the "Next" target Y for both sides
        // We iterate Y until we hit the bottom of the bounding box
        // But we must switch edges when Y passes a vertex.

        // We need a loop that goes row by row.
        // We stop when we pass the lowest vertex.
        // To do this simply, we just track "Current Target Y" for left and right.

        ScanlineCore(corners, topIdx, leftIdx, rightIdx, startY, clipMinY, clipMaxY, onScanline);
    }


    private static void ScanlineCore(
        ReadOnlySpan<Vector2> corners,
        int topIdx, int leftIdx, int rightIdx,
        int currentY, int clipMinY, int clipMaxY,
        Action<int, float, float> callback)
    {
        // Helper to get next vertex index down the chain
        // direction: -1 for left side (usually), +1 for right side
        int NextVert(int idx, int dir) => (idx + dir + 4) % 4;

        // Setup Left Edge
        Vector2 vL1 = corners[topIdx];
        Vector2 vL2 = corners[leftIdx];

        // Handle horizontal top edge on Left
        while (vL2.Y <= vL1.Y)
        {
            vL1 = vL2;
            leftIdx = NextVert(leftIdx, -1);
            vL2 = corners[leftIdx];
            if (leftIdx == topIdx) return;
        }

        float slopeL = (vL2.X - vL1.X) / (vL2.Y - vL1.Y);

        // Setup Right Edge
        Vector2 vR1 = corners[topIdx];
        Vector2 vR2 = corners[rightIdx];

        // Handle horizontal top edge on Right
        while (vR2.Y <= vR1.Y)
        {
            vR1 = vR2;
            rightIdx = NextVert(rightIdx, 1);
            vR2 = corners[rightIdx];
            if (rightIdx == topIdx) return;
        }

        float slopeR = (vR2.X - vR1.X) / (vR2.Y - vR1.Y);

        // Correction for the first sub-pixel step
        float xL = vL1.X + (currentY - vL1.Y) * slopeL;
        float xR = vR1.X + (currentY - vR1.Y) * slopeR;

        // We need the bottom-most Y to know when to stop globally
        // But we just loop until both sides are exhausted.
        // For a rect, we have 3 stages max (Top triangle, Middle, Bottom triangle).

        // Find the global bottom Y to stop the loop
        float totalMaxY = corners[0].Y;
        for (int i = 1; i < 4; i++)
            if (corners[i].Y > totalMaxY)
                totalMaxY = corners[i].Y;
        int endY = (int)Math.Floor(totalMaxY);

        // Clamp to viewport
        if (currentY < clipMinY)
        {
            // Skip ahead
            int skip = clipMinY - currentY;
            currentY = clipMinY;
            xL += slopeL * skip;
            xR += slopeR * skip;
        }

        if (endY > clipMaxY) endY = clipMaxY;

        for (; currentY <= endY; currentY++)
        {
            // 1. Check if we passed the Left "Knee"
            if (currentY > vL2.Y)
            {
                // Switch to next edge on left
                vL1 = vL2;
                // Depending on winding, the next left point is further along -1 or +1
                // For a rect, if we went (top-1), we continue -1.
                leftIdx = NextVert(leftIdx, -1);
                // If we wrapped around to the right side's target, we are done with left,
                // but for a rect, we just hit the bottom vertex.
                vL2 = corners[leftIdx];

                // Recalculate slope
                slopeL = (vL2.X - vL1.X) / (vL2.Y - vL1.Y);
                // Recalculate X to eliminate drift error
                xL = vL1.X + (currentY - vL1.Y) * slopeL;
            }

            // 2. Check if we passed the Right "Knee"
            if (currentY > vR2.Y)
            {
                vR1 = vR2;
                rightIdx = NextVert(rightIdx, 1);
                vR2 = corners[rightIdx];
                slopeR = (vR2.X - vR1.X) / (vR2.Y - vR1.Y);
                xR = vR1.X + (currentY - vR1.Y) * slopeR;
            }

            // 3. Emit Scanline
            callback(currentY, xL, xR);

            // 4. Increment (DDA)
            xL += slopeL;
            xR += slopeR;
        }
    }
}