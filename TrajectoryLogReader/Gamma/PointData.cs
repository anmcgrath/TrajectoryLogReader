namespace TrajectoryLogReader.Gamma;

internal class PointData
{
    public double X { get; }
    public double Y { get; }
    public double Value { get; }

    public PointData(double x, double y, double value)
    {
        X = x;
        Y = y;
        Value = value;
    }
}