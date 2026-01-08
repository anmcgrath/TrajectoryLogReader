namespace TrajectoryLogReader.Gamma;

internal class Offset
{
    public int XIndexOffset { get; }
    public int YIndexOffset { get; }
    public double DistSquared { get; }

    public Offset(int xIndexOffset, int yIndexOffset, double distSquared)
    {
        XIndexOffset = xIndexOffset;
        YIndexOffset = yIndexOffset;
        DistSquared = distSquared;
    }
}