using System.Numerics;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Fluence;

public class FluenceCreator
{
    private readonly MeasurementDataCollection _data;
    private readonly TrajectoryLog _log;

    internal FluenceCreator(MeasurementDataCollection data, TrajectoryLog log)
    {
        _data = data;
        _log = log;
    }

    public FieldFluence Create(FluenceOptions options, RecordType recordType)
    {
        var maxExtent = CalculateMaxExtentX(recordType);
        var w = options.GridSizeXInCm <= 0 ? maxExtent.X : options.GridSizeXInCm;
        var h = options.GridSizeYInCm <= 0 ? maxExtent.Y : options.GridSizeYInCm;

        var grid = new GridF(
            w,
            h,
            options.GridCountX,
            options.GridCountY);

        var time = options.SampleRateInMs;
        // ensure time is a multiple of the sampling rate
        time = (int)Math.Round((double)time / _log.Header.SamplingIntervalInMS)
               * _log.Header.SamplingIntervalInMS;
        // but not zero
        time = Math.Max(time, _log.Header.SamplingIntervalInMS);

        Span<Vector2> corners = stackalloc Vector2[4];

        var prevMu = _data.First().MU.GetRecord(recordType);
        foreach (var s in _data)
        {
            if (s.TimeInMs % time != 0)
                continue;

            var deltaMu = s.MU.GetRecord(recordType) - prevMu;
            prevMu = s.MU.GetRecord(recordType);
            if (deltaMu <= 0)
                continue;

            var x1 = -s.X1.GetRecord(recordType);
            var y1 = -s.Y1.GetRecord(recordType);
            var x2 = s.X2.GetRecord(recordType);
            var y2 = s.Y2.GetRecord(recordType);
            var coll = Scale.Convert(
                _log.Header.AxisScale,
                AxisScale.ModifiedIEC61217, Axis.CollRtn,
                s.CollRtn.GetRecord(recordType));

            var mlc = _log.MlcModel;

            var leafPositions = recordType == RecordType.ActualPosition ? s.MLC.Actual : s.MLC.Expected;

            var angleRadians = coll * (float)Math.PI / 180;

#if NET7_0_OR_GREATER
            var (sin, cos) = MathF.SinCos(angleRadians);
#else
            var sin = (float)Math.Sin(angleRadians);
            var cos = (float)Math.Cos(angleRadians);
#endif

            for (int i = 0; i < _log.Header.GetNumberOfLeafPairs(); i++)
            {
                var bankAPos = leafPositions[1, i];
                var bankBPos = -leafPositions[0, i];

                if (bankBPos < x1 && bankAPos < x1)
                    continue;

                if (bankBPos > x2 && bankAPos > x2)
                    continue;

                bankBPos = Math.Max(bankBPos, x1);
                bankAPos = Math.Min(bankAPos, x2);

                var leafInfo = mlc.GetLeafInformation(i);

                // Working in CM
                var leafWidthCm = leafInfo.WidthInMm / 10f;
                var leafCenterYCm = leafInfo.YInMm / 10f;
                var yMinCm = leafCenterYCm - leafWidthCm / 2f;
                var yMaxCm = leafCenterYCm + leafWidthCm / 2f;

                // Constrain to jaw positions
                if (yMinCm < y1)
                    yMinCm = y1;
                if (yMaxCm < y1)
                    yMaxCm = y1;
                if (yMinCm > y2)
                    yMinCm = y2;
                if (yMaxCm > y2)
                    yMaxCm = y2;

                if (Math.Abs(yMinCm - yMaxCm) < 0.0001)
                    continue;

                var width = bankAPos - bankBPos;
                var xCenter = bankBPos + width / 2f;
                var yCenter = leafCenterYCm;

                // Rotate the center of the leaf around (0,0) to account for collimator rotation
                var xRot = xCenter * cos - yCenter * sin;
                var yRot = xCenter * sin + yCenter * cos;

                FastRotatedRect.GetRotatedRectAndBounds(
                    new Vector2(xRot, yRot),
                    width,
                    leafWidthCm,
                    cos, sin, corners, out var bounds);

                grid.DrawDataFast(corners, bounds, deltaMu);
            }
        }

        return new FieldFluence(grid);
    }

    private Point CalculateMaxExtentX(RecordType recordType)
    {
        var xExtent = double.MinValue;
        var yExtent = double.MinValue;
        foreach (var d in _data)
        {
            var x1 = d.X1.GetRecord(recordType);
            var y1 = d.Y1.GetRecord(recordType);
            var x2 = d.X2.GetRecord(recordType);
            var y2 = d.Y2.GetRecord(recordType);
            var coll = d.CollRtn.GetRecord(recordType) * Math.PI / 180;

            // c2 ---Y2-- c1
            //  |         |
            //  X1       X2
            //  |         |
            // c3 --Y1---c4

            var c1Dist = Math.Sqrt(x2 * x2 + y2 * y2);
            var c2Dist = Math.Sqrt(x1 * x1 + y2 * y2);
            var c3Dist = Math.Sqrt(x1 * x1 + y1 * y1);
            var c4Dist = Math.Sqrt(x1 * x1 + y1 * y1);

            var d1 = Math.Max(c1Dist, c3Dist);
            var d2 = Math.Max(c2Dist, c4Dist);
            var cosCol = Math.Abs(Math.Cos(Math.PI / 4 - coll));
            var sinCol = Math.Abs(Math.Sin(Math.PI / 4 - coll));
            var maxX = Math.Max(cosCol * d1, sinCol * d2);
            var maxY = Math.Max(cosCol * d2, sinCol * d1);
            xExtent = Math.Max(maxX, xExtent);
            yExtent = Math.Max(maxY, yExtent);
        }

        return new Point(xExtent * 2, yExtent * 2);
    }
}