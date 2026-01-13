using TrajectoryLogReader.Extensions;

namespace TrajectoryLogReader.LogStatistics;

public class Histogram
{
    public int[] Counts { get; private set; }
    public float[] BinStarts { get; private set; }

    public Histogram(int[] counts, float[] binStarts)
    {
        Counts = counts;
        BinStarts = binStarts;
    }

    public static Histogram FromData(float[] data, int nBins)
    {
        var (min, max) = data.MinMax();
        var range = max - min;
        var counts = new int[nBins];
        float binSize = range / nBins;
        var binStarts = Enumerable.Range(0, nBins).Select(x => x * binSize + min).ToArray();

        for (int i = 0; i < data.Length; i++)
        {
            int index = (int)((data[i] - min) / binSize);

            if (index < 0) index = 0;
            else if (index >= nBins) index = nBins - 1;

            counts[index]++;
        }

        return new Histogram(counts, binStarts);
    }
}