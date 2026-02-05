using System.Numerics;
using TrajectoryLogReader.Fluence.Adapters;
using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Snapshots;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Fluence;

/// <summary>
/// Reconstructs a 2D fluence map by accumulating MU-weighted leaf openings over time.
/// Conceptually, each snapshot contributes dose proportional to its <c>DeltaMu</c>,
/// and the union of those contributions yields a delivery-derived fluence estimate.
/// </summary>
public class FluenceCreator
{
    public FluenceCreator()
    {
    }

    /// <summary>
    /// Creates a fluence map from trajectory-log snapshots. This overload allows you to
    /// select expected vs. actual positions and down-sample the log for speed.
    /// </summary>
    /// <param name="options">Fluence generation options.</param>
    /// <param name="recordType">Expected or Actual position.</param>
    /// <param name="samplingRateInMs">
    /// The time between sampled snapshots in milliseconds. Larger values reduce noise and
    /// computation but can smooth away transient delivery details. The value is coerced to
    /// a multiple of the log's sampling interval.
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
    /// Creates a fluence map from any IEC-consistent delivery snapshot source.
    /// This is the primary entry point for both log-derived and plan-derived fluence.
    /// </summary>
    /// <param name="options">Fluence generation options.</param>
    /// <param name="fieldData">The field data source.</param>
    /// <returns>A <see cref="FieldFluence"/> object.</returns>
    public FieldFluence Create(FluenceOptions options, IFieldDataCollection fieldData)
    {
        var data = fieldData.ToList();
        var maxExtent = CalculateMaxExtent(data, out var jawOutlines, options);

        // Apply margin to the calculated bounds
        var bounds = new Rect(
            maxExtent.X - options.Margin,
            maxExtent.Y - options.Margin,
            maxExtent.Width + 2 * options.Margin,
            maxExtent.Height + 2 * options.Margin);

        // Override with user-specified dimensions if provided (centered at origin)
        if (options.Width > 0)
        {
            bounds.X = -options.Width / 2;
            bounds.Width = options.Width;
        }

        if (options.Height > 0)
        {
            bounds.Y = -options.Height / 2;
            bounds.Height = options.Height;
        }

        var grid = new GridF(bounds, options.Cols, options.Rows);

        // Prepare work items
        var workItems = new List<(IFieldData s, float deltaMu)>();

        foreach (var s in data)
        {
            if (s.DeltaMu > options.MinDeltaMu && (!options.ExcludeBeamHolds || !s.IsBeamHold()))
                workItems.Add((s, s.DeltaMu));
        }

        var useApproximate = options.UseApproximateFluence;

        Parallel.ForEach(workItems,
            new ParallelOptions()
            {
                MaxDegreeOfParallelism = options.MaxParallelism
            },
            () => new GridF(bounds, options.Cols, options.Rows),
            (item, loopState, localGrid) => ProcessLocalGrid(options, item, localGrid, useApproximate),
            (localGrid) =>
            {
                lock (grid)
                {
                    grid.Add(localGrid);
                }
            });

        return new FieldFluence(grid, options, jawOutlines);
    }

    private static GridF ProcessLocalGrid(FluenceOptions options, (IFieldData s, float deltaMu) item, GridF localGrid,
        bool useApproximate)
    {
        var s = item.s;
        var deltaMu = item.deltaMu;

        var x1 = s.X1InMm;
        var y1 = s.Y1InMm;
        var x2 = s.X2InMm;
        var y2 = s.Y2InMm;
        var coll = options.FixedCollimatorAngle ?? s.CollimatorInDegrees;
        var mlc = s.Mlc;

        var angleRadians = (float)(coll * Math.PI / 180);

#if NET7_0_OR_GREATER
        var (sin, cos) = MathF.SinCos(angleRadians);
#else
        var sin = (float)Math.Sin(angleRadians);
        var cos = (float)Math.Cos(angleRadians);
#endif
        Span<Vector2> corners = stackalloc Vector2[4];
        var leafPairCount = mlc.GetNumberOfLeafPairs();

        for (int i = 0; i < leafPairCount; i++)
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
            yMinMm = Math.Max(yMinMm, y1);
            yMinMm = Math.Min(yMinMm, y2);
            yMaxMm = Math.Max(yMaxMm, y1);
            yMaxMm = Math.Min(yMaxMm, y2);

            var height = yMaxMm - yMinMm;
            if (height < 0.0001)
                continue;

            var xCenter = bankBPos + width / 2f;
            var yCenter = (yMinMm + yMaxMm) / 2f;

            // Rotate the center of the leaf around (0,0) to account for collimator rotation
            var xRot = xCenter * cos - yCenter * sin;
            var yRot = xCenter * sin + yCenter * cos;

            RotatedRect.GetRotatedRectAndBounds(
                new Vector2(xRot, yRot),
                width,
                height,
                cos, sin, corners, out var leafBounds);

            localGrid.DrawData(corners, leafBounds, deltaMu, useApproximate);
        }

        return localGrid;
    }

    /// <summary>
    /// Calculates the jaw-driven spatial extent required to cover all rotated field outlines.
    /// This ensures the fluence grid is large enough even when collimator rotation expands
    /// the axis-aligned bounding box.
    /// </summary>
    private Rect CalculateMaxExtent(IEnumerable<IFieldData> fieldData, out List<Point[]> jawOutlines,
        FluenceOptions options)
    {
        var minX = double.MaxValue;
        var maxX = double.MinValue;
        var minY = double.MaxValue;
        var maxY = double.MinValue;

        // Track the configuration that produces the maximum extent (by bounding box area)
        double maxArea = 0;
        Point[] maxExtentCorners = null!;

        foreach (var d in fieldData)
        {
            var x1 = d.X1InMm;
            var y1 = d.Y1InMm;
            var x2 = d.X2InMm;
            var y2 = d.Y2InMm;
            var collDegrees = options.FixedCollimatorAngle ?? d.CollimatorInDegrees;
            var coll = collDegrees * Math.PI / 180;

            var cos = Math.Cos(coll);
            var sin = Math.Sin(coll);

            // Jaw corners in collimator coordinates:
            // c2 ---Y2-- c1
            //  |         |
            //  X1       X2
            //  |         |
            // c3 --Y1---c4

            // Rotate each corner by collimator angle
            var c1 = RotatePoint(x2, y2, cos, sin);
            var c2 = RotatePoint(x1, y2, cos, sin);
            var c3 = RotatePoint(x1, y1, cos, sin);
            var c4 = RotatePoint(x2, y1, cos, sin);

            // Calculate bounding box for this configuration
            var configMinX = Math.Min(Math.Min(c1.X, c2.X), Math.Min(c3.X, c4.X));
            var configMaxX = Math.Max(Math.Max(c1.X, c2.X), Math.Max(c3.X, c4.X));
            var configMinY = Math.Min(Math.Min(c1.Y, c2.Y), Math.Min(c3.Y, c4.Y));
            var configMaxY = Math.Max(Math.Max(c1.Y, c2.Y), Math.Max(c3.Y, c4.Y));

            var area = (configMaxX - configMinX) * (configMaxY - configMinY);
            if (area > maxArea)
            {
                maxArea = area;
                maxExtentCorners = new[] { c1, c2, c3, c4 };
            }

            // Update overall bounds
            minX = Math.Min(minX, configMinX);
            maxX = Math.Max(maxX, configMaxX);
            minY = Math.Min(minY, configMinY);
            maxY = Math.Max(maxY, configMaxY);
        }

        // Use the rotated jaw outline from the configuration with maximum extent
        jawOutlines = maxExtentCorners != null
            ? new List<Point[]> { maxExtentCorners }
            : new List<Point[]>();

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private static Point RotatePoint(double x, double y, double cos, double sin)
    {
        return new Point(x * cos - y * sin, x * sin + y * cos);
    }
}