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

        float sumSq = 0;
        foreach (var data in _data)
        {
            var delta = data.GetScalarRecord(axis).Delta;
            sumSq += delta * delta;
        }

        return (float)Math.Sqrt(sumSq / _data.Count);
    }

    private float RootMeanSquareErrorMlcs()
    {
        float sumSq = 0;
        foreach (var data in _data)
        {
            for (int leafIndex = 0; leafIndex < _log.Header.GetNumberOfLeafPairs(); leafIndex++)
            {
                var deltaA = data.MLC.Delta(leafIndex, 1);
                var deltaB = data.MLC.Delta(leafIndex, 0);
                sumSq += deltaA * deltaA + deltaB * deltaB;
            }
        }

        return (float)Math.Sqrt(sumSq / (_log.Header.NumberOfSnapshots * _log.Header.GetNumberOfLeafPairs() * 2));
    }
}