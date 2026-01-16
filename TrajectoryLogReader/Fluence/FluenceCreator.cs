using System.Numerics;
using TrajectoryLogReader.Fluence.Adapters;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Snapshots;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Fluence;

/// <summary>
/// Creates 2D fluence maps from trajectory log data.
/// </summary>
public class FluenceCreator
{
    public FluenceCreator()
    {
    }

    /// <summary>
    /// Creates a fluence map from measurement data.
    /// </summary>
    /// <param name="options">Fluence generation options.</param>
    /// <param name="recordType">Expected or Actual position.</param>
    /// <param name="samplingRateInMs">
    ///     The number of ms between each sample. Default is 20.
    ///     Set to higher for less accurate but faster fluence generation.
    ///     Must be a multiple of the log file sampling rate
    /// </param>
    /// <param name="data">The measurement data.</param>
    /// <returns>A <see cref="FieldFluence"/> object.</returns>
    internal FieldFluence Create(FluenceOptions options, RecordType recordType, double samplingRateInMs,
        SnapshotCollection data)
    {
        var sampleRate = samplingRateInMs;
        // ensure time is a multiple of the sampling rate
        sampleRate = (int)Math.Round((double)sampleRate / data.Log.Header.SamplingIntervalInMS)
                     * data.Log.Header.SamplingIntervalInMS;
        // but not zero
        sampleRate = Math.Max(sampleRate, data.Log.Header.SamplingIntervalInMS);

        var measAdapter = new MeasurementDataCollectionAdapter(data, recordType, sampleRate);
        return Create(options, measAdapter);
    }

    /// <summary>
    /// Creates a fluence map from a generic field data collection.
    /// </summary>
    /// <param name="options">Fluence generation options.</param>
    /// <param name="fieldData">The field data source.</param>
    /// <returns>A <see cref="FieldFluence"/> object.</returns>
    public FieldFluence Create(FluenceOptions options, IFieldDataCollection fieldData)
    {
        var data = fieldData.ToList();
        var maxExtent = CalculateMaxExtentX(data);
        var w = options.Width <= 0 ? maxExtent.X : options.Width;
        var h = options.Height <= 0 ? maxExtent.Y : options.Height;

        var grid = new GridF(
            w,
            h,
            options.Cols,
            options.Rows);

        // Prepare work items
        var workItems = new List<(IFieldData s, float deltaMu)>();

        foreach (var s in data)
        {
            if (s.DeltaMu > options.MinDeltaMu)
                workItems.Add((s, s.DeltaMu));
        }

        var useApproximate = options.UseApproximateFluence;

        Parallel.ForEach(workItems,
            () => new GridF(w, h, options.Cols, options.Rows),
            (item, loopState, localGrid) =>
            {
                var s = item.s;
                var deltaMu = item.deltaMu;
                Span<Vector2> corners = stackalloc Vector2[4];

                var x1 = s.X1InMm;
                var y1 = s.Y1InMm;
                var x2 = s.X2InMm;
                var y2 = s.Y2InMm;
                var coll = s.CollimatorInDegrees;
                var mlc = s.Mlc;

                var angleRadians = (float)(coll * Math.PI / 180);

#if NET7_0_OR_GREATER
                var (sin, cos) = MathF.SinCos(angleRadians);
#else
                var sin = (float)Math.Sin(angleRadians);
                var cos = (float)Math.Cos(angleRadians);
#endif

                for (int i = 0; i < mlc.GetNumberOfLeafPairs(); i++)
                {
                    var bankAPos = s.GetLeafPositionInMm(0, i);
                    var bankBPos = s.GetLeafPositionInMm(1, i);

                    bankBPos = Math.Max(bankBPos, x1);
                    bankBPos = Math.Min(bankBPos, x2);
                    bankAPos = Math.Max(bankAPos, x1);
                    bankAPos = Math.Min(bankAPos, x2);
                    
                    var width = bankAPos - bankBPos;
                    if (width <= 0)
                        continue;

                    var leafInfo = mlc.GetLeafInformation(i);

                    // Working in Mm
                    var leafWidthMm = leafInfo.WidthInMm;
                    var leafCenterYMm = leafInfo.YInMm;
                    var yMinMm = leafCenterYMm - leafWidthMm / 2f;
                    var yMaxMm = leafCenterYMm + leafWidthMm / 2f;

                    // Constrain to jaw positions
                    if (yMinMm < y1)
                        yMinMm = y1;
                    if (yMaxMm < y1)
                        yMaxMm = y1;
                    if (yMinMm > y2)
                        yMinMm = y2;
                    if (yMaxMm > y2)
                        yMaxMm = y2;

                    // both outside y jaw
                    if (Math.Abs(yMinMm - yMaxMm) < 0.0001)
                        continue;

                    var xCenter = bankBPos + width / 2f;
                    var yCenter = leafCenterYMm;

                    // Rotate the center of the leaf around (0,0) to account for collimator rotation
                    var xRot = xCenter * cos - yCenter * sin;
                    var yRot = xCenter * sin + yCenter * cos;

                    RotatedRect.GetRotatedRectAndBounds(
                        new Vector2(xRot, yRot),
                        width,
                        leafWidthMm,
                        cos, sin, corners, out var bounds);

                    localGrid.DrawData(corners, bounds, deltaMu, useApproximate);
                }

                return localGrid;
            },
            (localGrid) =>
            {
                lock (grid)
                {
                    grid.Add(localGrid);
                }
            });

        return new FieldFluence(grid, options);
    }

    private Point CalculateMaxExtentX(IEnumerable<IFieldData> fieldData)
    {
        var xExtent = double.MinValue;
        var yExtent = double.MinValue;
        foreach (var d in fieldData)
        {
            var x1 = d.X1InMm;
            var y1 = d.Y1InMm;
            var x2 = d.X2InMm;
            var y2 = d.Y2InMm;
            var coll = d.CollimatorInDegrees * Math.PI / 180;

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