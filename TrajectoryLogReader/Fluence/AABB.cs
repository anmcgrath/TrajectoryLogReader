namespace TrajectoryLogReader.Fluence;

// A simple struct to represent the Axis Aligned Rect
internal struct AABB
{
    public readonly double MinX;
    public readonly double MinY;
    public readonly double MaxX;
    public readonly double MaxY;

    public AABB(double minX, double minY, double maxX, double maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }
}