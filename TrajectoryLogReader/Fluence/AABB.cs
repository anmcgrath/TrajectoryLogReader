namespace TrajectoryLogReader.Fluence;

// A simple struct to represent the Axis Aligned Rect
internal struct AABB
{
    public float MinX;
    public float MinY;
    public float MaxX;
    public float MaxY;

    public AABB(float minX, float minY, float maxX, float maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }
}