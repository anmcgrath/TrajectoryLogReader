using TrajectoryLogReader.Extensions;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.LogStatistics;

public class Statistics
{
    private readonly MeasurementDataCollection _data;
    private readonly TrajectoryLog _log;

    internal Statistics(MeasurementDataCollection data, TrajectoryLog log)
    {
        _data = data;
        _log = log;
    }

    /// <summary>
    /// Returns the RMS of errors for the axis <paramref name="axis"/> (actual - expected).
    /// For the MLC axis, this returns the error over all MLCs
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public float RootMeanSquareError(Axis axis)
    {
        if (axis == Axis.MLC)
            return RootMeanSquareErrorMlcs();

        var axisDataObj = _log.GetAxisData(axis);
        if (axisDataObj == null)
            return 0f;

        var data = axisDataObj.Data;
        int count = axisDataObj.NumSnapshots;
        if (count == 0)
            return 0;

        int stride = axisDataObj.SamplesPerSnapshot; // Should be 2 for Scalar axes (Expected, Actual)

        if (stride < 2) return 0f;

        bool isRotational = axis == Axis.GantryRtn || axis == Axis.CollRtn || axis == Axis.CouchRtn;
        double sumSq = 0;

        for (int i = 0; i < count; i++)
        {
            int baseIdx = i * stride;
            // Index 0 is Expected, Index 1 is Actual
            float expected = data[baseIdx];
            float actual = data[baseIdx + 1];
            float diff = actual - expected;

            if (isRotational)
            {
                // Handle wrap-around for rotational axes (e.g. 359 vs 1 degree)
                if (diff < -180) diff += 360;
                else if (diff >= 180) diff -= 360;
            }

            sumSq += diff * diff;
        }

        return (float)Math.Sqrt(sumSq / count);
    }

    /// <summary>
    /// Returns the error with the largest magnitude for the given axis (MLC not included)
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public float MaxError(Axis axis)
    {
        if (axis == Axis.MLC)
            return MaxErrorMlcs();

        var axisDataObj = _log.GetAxisData(axis);
        if (axisDataObj == null)
            return 0f;

        var data = axisDataObj.Data;
        int count = axisDataObj.NumSnapshots;
        int stride = axisDataObj.SamplesPerSnapshot;

        if (stride < 2) return 0f;

        bool isRotational = axis == Axis.GantryRtn || axis == Axis.CollRtn || axis == Axis.CouchRtn;
        float maxError = 0f;
        float maxErrorAbs = 0f;

        for (int i = 0; i < count; i++)
        {
            int baseIdx = i * stride;
            float expected = data[baseIdx];
            float actual = data[baseIdx + 1];
            float diff = actual - expected;

            if (isRotational)
            {
                if (diff < -180) diff += 360;
                else if (diff >= 180) diff -= 360;
            }

            if (Math.Abs(diff) > maxErrorAbs)
            {
                maxError = diff;
                maxErrorAbs = Math.Abs(diff);
            }
        }

        return maxError;
    }

    private float MaxErrorMlcs()
    {
        float maxError = 0f;
        float maxErrorAbs = 0f;

        foreach (var data in _data)
        {
            for (int leafIndex = 0; leafIndex < _log.Header.GetNumberOfLeafPairs(); leafIndex++)
            {
                var deltaA = data.MLC.GetDelta(1, leafIndex);
                var deltaB = data.MLC.GetDelta(0, leafIndex);
                if (Math.Abs(deltaA) > maxErrorAbs)
                {
                    maxErrorAbs = Math.Abs(deltaA);
                    maxError = deltaA;
                }

                if (Math.Abs(deltaB) > maxErrorAbs)
                {
                    maxErrorAbs = Math.Abs(deltaB);
                    maxError = deltaB;
                }
            }
        }

        return maxError;
    }

    private float RootMeanSquareErrorMlcs()
    {
        float sumSq = 0;
        foreach (var data in _data)
        {
            for (int leafIndex = 0; leafIndex < _log.Header.GetNumberOfLeafPairs(); leafIndex++)
            {
                var deltaA = data.MLC.GetDelta(1, leafIndex);
                var deltaB = data.MLC.GetDelta(0, leafIndex);
                sumSq += deltaA * deltaA + deltaB * deltaB;
            }
        }

        return (float)Math.Sqrt(sumSq / (_log.Header.NumberOfSnapshots * _log.Header.GetNumberOfLeafPairs() * 2));
    }

    /// <summary>
    /// Returns the RMS error for a specific MLC leaf.
    /// </summary>
    /// <param name="bank">Bank index (0 or 1)</param>
    /// <param name="leafIndex">Leaf index</param>
    /// <returns>RMS error</returns>
    public float RootMeanSquareError(int bank, int leafIndex)
    {
        float sumSq = 0;
        foreach (var data in _data)
        {
            var delta = data.MLC.GetDelta(bank, leafIndex);
            sumSq += delta * delta;
        }

        return (float)Math.Sqrt(sumSq / _data.Count);
    }

    /// <summary>
    /// Returns the max error for a specific MLC leaf.
    /// </summary>
    /// <param name="bank">Bank index (0 or 1)</param>
    /// <param name="leafIndex">Leaf index</param>
    /// <returns>Max absolute error</returns>
    public float MaxError(int bank, int leafIndex)
    {
        float maxError = 0f;
        foreach (var data in _data)
        {
            var delta = data.MLC.GetDelta(bank, leafIndex);
            if (Math.Abs(delta) > Math.Abs(maxError))
            {
                maxError = delta;
            }
        }

        return maxError;
    }

    /// <summary>
    /// Creates an histogram of errors for <paramref name="axis"/>
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="nBins">The number of bins in the histogram</param>
    /// <returns></returns>
    public Histogram ErrorHistogram(Axis axis, int nBins = 20)
    {
        return Histogram.FromData(_data.Select(x => x.GetScalarRecord(axis).Delta)
            .ToArray(), nBins);
    }

    /// <summary>
    /// Creates an histogram of single MLC errors for leaf <paramref name="leafIndex"/> on bank <paramref name="bankIndex"/>
    /// </summary>
    /// <param name="bankIndex"></param>
    /// <param name="leafIndex"></param>
    /// <param name="nBins">The number of bins in the histogram</param>
    /// <returns></returns>
    public Histogram ErrorHistogram(int bankIndex, int leafIndex, int nBins = 20)
    {
        return Histogram.FromData(_data.Select(x => x.MLC.GetDelta(leafIndex, bankIndex))
            .ToArray(), nBins);
    }
}