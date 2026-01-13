namespace TrajectoryLogReader.Extensions;

public static class MathExtensions
{
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