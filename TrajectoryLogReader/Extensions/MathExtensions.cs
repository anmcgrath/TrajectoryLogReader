namespace TrajectoryLogReader.Extensions;

public static class MathExtensions
{
    /// <summary>
    /// Returns the minimum and maximum of <paramref name="values"/>
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public static (float min, float max) MinMax(this float[] values)
    {
        if (values.Length == 0)
            throw new IndexOutOfRangeException($"Values is empty");

        var min = float.MaxValue;
        var max = float.MinValue;

        for (var i = 0; i < values.Length; i++)
        {
            min = Math.Min(values[i], min);
            max = Math.Max(values[i], max);
        }

        return (min, max);
    }
}