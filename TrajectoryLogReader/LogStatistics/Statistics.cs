using TrajectoryLogReader.Extensions;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.LogStatistics;

public class Statistics
{
    private readonly SnapshotCollection _data;
    private readonly TrajectoryLog _log;

    internal Statistics(SnapshotCollection data, TrajectoryLog log)
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

    /// <summary>
    /// Bins snapshot data by another axis. E.g calculate error in MLC position by gantry angle size etc. Note that binned data is in the IEC scale
    /// </summary>
    /// <param name="valueSelector"></param>
    /// <param name="binAxis">The axis to bin on e.g gantry angle</param>
    /// <param name="binRecordType">The record type to bin on</param>
    /// <param name="min">The minimum of the bin axis value. If the binned axis value is outside the range, data won't be aggregated</param>
    /// <param name="max">The maximum of the bin axis value. If the binned axis value is outside the range, data won't be aggregated</param>
    /// <param name="binSize"></param>
    /// <param name="aggregator">The aggregate function, defaults to sum.</param>
    /// <returns></returns>
    public BinnedData BinByAxis(Func<Snapshot, float> valueSelector, Axis binAxis, RecordType binRecordType, float min,
        float max, float binSize, Func<float, float, float>? aggregator = null)
    {
        if (aggregator == null)
            aggregator = (aggregate, val) => (aggregate + val);

        var range = max - min;
        int nBins = (int)(range / binSize) + 1;
        var values = new float[nBins];
        var binStarts = Enumerable.Range(0, nBins).Select(x => x * binSize + min).ToArray();

        foreach (var snapshot in _data)
        {
            var binData = snapshot.GetScalarRecord(binAxis).GetRecordInIec(binRecordType);
            var data = valueSelector(snapshot);
            int index = (int)((binData - min) / binSize);

            if (index < 0) index = 0;
            else if (index >= nBins) index = nBins - 1;

            values[index] = aggregator(values[index], data);
        }

        return new BinnedData(values, binStarts);
    }

    /// <summary>
    /// Returns the total MU delivered per gantry angle for the data collection.
    /// </summary>
    /// <param name="gantryBinSize"></param>
    /// <returns></returns>
    public BinnedData GetMuPerGantryAngle(float gantryBinSize, RecordType recordType = RecordType.ActualPosition)
    {
        float DeltaMuSelector(Snapshot s)
        {
            var previous = s.Previous();
            var prevMu = previous?.MU.GetRecord(recordType) ?? 0f;
            return s.MU.GetRecord(recordType) - prevMu;
        }

        return BinByAxis(DeltaMuSelector, Axis.GantryRtn, recordType, 0, 360, gantryBinSize);
    }
}