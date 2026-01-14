namespace TrajectoryLogReader.LogStatistics;

public class BinnedData
{
    public float[] Values { get; private set; }
    public float[] Bins { get; private set; }

    public BinnedData(float[] values, float[] bins)
    {
        Values = values;
        Bins = bins;
    }
}