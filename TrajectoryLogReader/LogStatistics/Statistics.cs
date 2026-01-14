using TrajectoryLogReader.Extensions;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Snapshots;
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
    /// <param name="recordType"></param>
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

    /// <summary>
    /// Calculates the Root Mean Square Error from a sequence of deltas.
    /// </summary>
    public static float CalculateRootMeanSquareError(IEnumerable<float> deltas)
    {
        double sumSq = 0;
        int count = 0;
        foreach (var diff in deltas)
        {
            sumSq += diff * diff;
            count++;
        }

        return count == 0 ? 0 : (float)Math.Sqrt(sumSq / count);
    }

    /// <summary>
    /// Calculates the Max Error (signed value with largest magnitude) from a sequence of deltas.
    /// </summary>
    public static float CalculateMaxError(IEnumerable<float> deltas)
    {
        float maxError = 0f;
        float maxErrorAbs = 0f;

        foreach (var diff in deltas)
        {
            if (Math.Abs(diff) > maxErrorAbs)
            {
                maxError = diff;
                maxErrorAbs = Math.Abs(diff);
            }
        }

        return maxError;
    }
}