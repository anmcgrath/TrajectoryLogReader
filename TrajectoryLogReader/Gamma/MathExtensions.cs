namespace TrajectoryLogReader.Gamma;

public static class MathExtensions
{
    /// <summary>
    /// Calculates the median of a series of floats but does not include <paramref name="ignoreValue"/>
    /// </summary>
    /// <param name="source"></param>
    /// <param name="ignoreValue"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static float Median(this IEnumerable<float> source, float ignoreValue = -1)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // Materialize the list so we can sort it efficiently
        var sortedList = source.Where(x => Math.Abs(x - ignoreValue) > 0.001).ToList();

        int count = sortedList.Count;
        if (count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        sortedList.Sort();

        int midIndex = count / 2;

        // If odd, return the middle element
        if (count % 2 != 0)
        {
            return sortedList[midIndex];
        }
        // If even, average the two middle elements
        else
        {
            return (sortedList[midIndex - 1] + sortedList[midIndex]) / 2f;
        }
    }
}