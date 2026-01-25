using System.Diagnostics;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Gamma;

/// <summary>
/// Calculates 2D Gamma index between two fluence maps.
/// </summary>
public static class GammaCalculator2D
{
    /// <summary>
    /// Perform a gamma comparison between two field fluences.
    /// </summary>
    /// <param name="parameters">Gamma parameters.</param>
    /// <param name="reference">Reference fluence.</param>
    /// <param name="compared">Compared fluence.</param>
    /// <returns>Gamma result.</returns>
    public static GammaResult2D Calculate(GammaParameters2D parameters, FieldFluence reference, FieldFluence compared)
    {
        return Calculate(parameters, new FluenceGridWrapper(reference.Grid), new FluenceGridWrapper(compared.Grid));
    }

    /// <summary>
    /// Perform a gamma comparison between two grids.
    /// </summary>
    /// <param name="parameters">The gamma parameters used for the comparison.</param>
    /// <param name="reference">The reference grid</param>
    /// <param name="compared">The grid that will be compared to the <paramref name="reference"/></param>
    /// <returns></returns>
    public static GammaResult2D Calculate(GammaParameters2D parameters, IGrid<float> reference, IGrid<float> compared)
    {
        var ctx = InitializeContext(parameters, reference, compared);

        ComputeInterpolationWeights(ref ctx, reference);
        ComputeColumnBounds(ref ctx, compared);
        ResampleReferenceGrid(ref ctx, reference);

        var (fracPass, gammaGrid) = ComputeGammaValues(ref ctx, parameters, compared);

        return new GammaResult2D(parameters, fracPass, gammaGrid);
    }

    /// <summary>
    /// Holds all shared state for the gamma calculation to avoid excessive parameter passing.
    /// </summary>
    private struct GammaContext
    {
        // Resampling parameters
        public double XSearchRes;
        public double YSearchRes;
        public double SearchRadMm;
        public int Mx;
        public int My;
        public int ResampledSizeX;
        public int ResampledSizeY;

        // Dose thresholds
        public double MaxRefDose;
        public double ThreshDose;

        // Resampled coordinate arrays
        public double[] ResampledX;
        public double[] ResampledY;

        // Resampled reference dose grid
        public double[,] ResampledRefDose;

        // Pre-computed interpolation data
        public int[] XIndices;
        public double[] XWeights;
        public int[] YIndices;
        public double[] YWeights;
        public int RefCols;
        public float[]? RefData;

        // Column bounds for resampled grid
        public int[] MinCol;
        public int[] MaxCol;

        // Column bounds for compared grid
        public int[] ComparedMinCol;
        public int[] ComparedMaxCol;

        // Search offsets
        public Offset[] Offsets;

        // Gamma criteria (squared)
        public double DoseCriteriaSq;
        public double DtaCriteriaSq;
    }

    private static GammaContext InitializeContext(GammaParameters2D parameters, IGrid<float> reference, IGrid<float> compared)
    {
        var searchRadMm = parameters.SearchRadius ?? parameters.DtaTolMm * 2;

        var samplingRate = Math.Min(parameters.SamplingRate, 10);

        var xSearchRes = parameters.DtaTolMm / samplingRate;
        var ySearchRes = parameters.DtaTolMm / samplingRate;

        // Calculate resampling multipliers
        // mx or my = the number of spaces between original dose points
        // e.g [ ]   x    x   [ ] has 3 spaces with 2 additional points at x
        var mx = (int)Math.Ceiling(compared.XRes / xSearchRes);
        var my = (int)Math.Ceiling(compared.YRes / ySearchRes);

        xSearchRes = compared.XRes / mx;
        ySearchRes = compared.YRes / my;

        var resampledSizeX = (mx - 1) * (compared.Cols - 1) + compared.Cols;
        var resampledSizeY = (my - 1) * (compared.Rows - 1) + compared.Rows;

        // Cache Max() values to avoid redundant array scans
        var comparedMax = compared.Max();
        var referenceMax = reference.Max();

        // Build resampled coordinate arrays
        var resampledX = new double[resampledSizeX];
        for (int i = 0; i < resampledSizeX; i++)
            resampledX[i] = compared.XMin + i * xSearchRes;

        var resampledY = new double[resampledSizeY];
        for (int i = 0; i < resampledSizeY; i++)
            resampledY[i] = compared.YMin + i * ySearchRes;

        return new GammaContext
        {
            XSearchRes = xSearchRes,
            YSearchRes = ySearchRes,
            SearchRadMm = searchRadMm,
            Mx = mx,
            My = my,
            ResampledSizeX = resampledSizeX,
            ResampledSizeY = resampledSizeY,
            MaxRefDose = Math.Max(comparedMax, referenceMax),
            ThreshDose = comparedMax * (parameters.ThresholdPercent / 100),
            ResampledX = resampledX,
            ResampledY = resampledY,
            ResampledRefDose = new double[resampledSizeY, resampledSizeX],
            XIndices = new int[resampledSizeX],
            XWeights = new double[resampledSizeX],
            YIndices = new int[resampledSizeY],
            YWeights = new double[resampledSizeY],
            RefCols = reference.Cols,
            RefData = reference is FluenceGridWrapper wrapper ? wrapper.Data : null,
            MinCol = new int[resampledSizeY],
            MaxCol = new int[resampledSizeY],
            ComparedMinCol = new int[compared.Rows],
            ComparedMaxCol = new int[compared.Rows],
            Offsets = GetOffsets(xSearchRes, ySearchRes, searchRadMm),
            DoseCriteriaSq = parameters.DoseTolPercent * parameters.DoseTolPercent,
            DtaCriteriaSq = parameters.DtaTolMm * parameters.DtaTolMm
        };
    }

    /// <summary>
    /// Pre-compute interpolation indices and weights for the resampled grid.
    /// This avoids expensive division and bounds checking in the inner loop.
    /// </summary>
    private static void ComputeInterpolationWeights(ref GammaContext ctx, IGrid<float> reference)
    {
        var refCols = reference.Cols;
        var refRows = reference.Rows;
        var refXMin = reference.XMin;
        var refYMin = reference.YMin;
        var refXRes = reference.XRes;
        var refYRes = reference.YRes;

        for (int i = 0; i < ctx.ResampledSizeX; i++)
        {
            double x = ctx.ResampledX[i];
            double colF = (x - refXMin) / refXRes;
            int col = (int)colF;
            if (col < 0)
            {
                col = 0;
                colF = 0;
            }
            else if (col >= refCols - 1)
            {
                col = refCols - 2;
                colF = refCols - 1;
            }

            ctx.XIndices[i] = col;
            ctx.XWeights[i] = colF - col;
        }

        for (int j = 0; j < ctx.ResampledSizeY; j++)
        {
            double y = ctx.ResampledY[j];
            double rowF = (y - refYMin) / refYRes;
            int row = (int)rowF;
            if (row < 0)
            {
                row = 0;
                rowF = 0;
            }
            else if (row >= refRows - 1)
            {
                row = refRows - 2;
                rowF = refRows - 1;
            }

            ctx.YIndices[j] = row;
            ctx.YWeights[j] = rowF - row;
        }
    }

    /// <summary>
    /// Pre-calculate the start/end column bounds for both the resampled and compared grids.
    /// This optimization limits the costly interpolation to only points relevant for comparison.
    /// </summary>
    private static void ComputeColumnBounds(ref GammaContext ctx, IGrid<float> compared)
    {
        // Initialize bounds arrays
        for (int k = 0; k < ctx.ResampledSizeY; k++)
        {
            ctx.MinCol[k] = int.MaxValue;
            ctx.MaxCol[k] = int.MinValue;
        }

        for (int k = 0; k < compared.Rows; k++)
        {
            ctx.ComparedMinCol[k] = int.MaxValue;
            ctx.ComparedMaxCol[k] = int.MinValue;
        }

        var nX = (int)(2 * ctx.SearchRadMm / ctx.XSearchRes) + 1;
        var nY = (int)(2 * ctx.SearchRadMm / ctx.YSearchRes) + 1;
        var marginX = nX / 2;
        var marginY = nY / 2;

        for (int yi = 0; yi < compared.Rows; yi++)
        {
            int minXi = -1, maxXi = -1;

            // Find min/max xi in this row where dose > threshold
            for (int xi = 0; xi < compared.Cols; xi++)
            {
                if (compared.GetValue(xi, yi) >= ctx.ThreshDose)
                {
                    if (minXi == -1) minXi = xi;
                    maxXi = xi;
                }
            }

            if (minXi == -1) continue;

            // Store bounds for the compared grid loop
            ctx.ComparedMinCol[yi] = minXi;
            ctx.ComparedMaxCol[yi] = maxXi;

            // Map to resampled coordinates
            int cY = yi * ctx.My;
            int startX = minXi * ctx.Mx;
            int endX = maxXi * ctx.Mx;

            // Apply search radius margins
            int startJ = Math.Max(0, cY - marginY);
            int endJ = Math.Min(ctx.ResampledSizeY - 1, cY + marginY);

            int rangeMinX = startX - marginX;
            int rangeMaxX = endX + marginX;

            for (int j = startJ; j <= endJ; j++)
            {
                if (rangeMinX < ctx.MinCol[j]) ctx.MinCol[j] = rangeMinX;
                if (rangeMaxX > ctx.MaxCol[j]) ctx.MaxCol[j] = rangeMaxX;
            }
        }
    }

    /// <summary>
    /// Resample the reference grid using pre-computed interpolation weights.
    /// </summary>
    private static void ResampleReferenceGrid(ref GammaContext ctx, IGrid<float> reference)
    {
        var refData = ctx.RefData;
        var refCols = ctx.RefCols;
        var xIndices = ctx.XIndices;
        var xWeights = ctx.XWeights;
        var yIndices = ctx.YIndices;
        var yWeights = ctx.YWeights;
        var minCol = ctx.MinCol;
        var maxCol = ctx.MaxCol;
        var resampledRefDose = ctx.ResampledRefDose;
        var resampledX = ctx.ResampledX;
        var resampledY = ctx.ResampledY;
        var resampledSizeX = ctx.ResampledSizeX;

        Parallel.For(0, ctx.ResampledSizeY, j =>
        {
            if (minCol[j] > maxCol[j]) return;

            int startI = Math.Max(0, minCol[j]);
            int endI = Math.Min(resampledSizeX - 1, maxCol[j]);

            int row = yIndices[j];
            double ty = yWeights[j];
            double tyInv = 1.0 - ty;

            if (refData != null)
            {
                // Fast path: direct array access with bilinear interpolation
                int row1Offset = row * refCols;
                int row2Offset = (row + 1) * refCols;

                for (int i = startI; i <= endI; i++)
                {
                    int col = xIndices[i];
                    double tx = xWeights[i];
                    double txInv = 1.0 - tx;

                    double f00 = refData[row1Offset + col];
                    double f10 = refData[row1Offset + col + 1];
                    double f01 = refData[row2Offset + col];
                    double f11 = refData[row2Offset + col + 1];

                    resampledRefDose[j, i] = tyInv * (txInv * f00 + tx * f10) + ty * (txInv * f01 + tx * f11);
                }
            }
            else
            {
                // Fallback: use interface method
                for (int i = startI; i <= endI; i++)
                {
                    resampledRefDose[j, i] = reference.Interpolate(resampledX[i], resampledY[j]);
                }
            }
        });
    }

    /// <summary>
    /// Compute gamma values for all points above threshold.
    /// </summary>
    private static (double fracPass, GridF gammaGrid) ComputeGammaValues(
        ref GammaContext ctx,
        GammaParameters2D parameters,
        IGrid<float> compared)
    {
        var gammaGrid = new GridF(
            compared.XMax - compared.XMin,
            compared.YMax - compared.YMin,
            compared.Cols,
            compared.Rows);

        // Initialize gamma grid with -1
        for (int i = 0; i < gammaGrid.Data.Length; i++)
            gammaGrid.Data[i] = -1;

        int numPass = 0;
        int ptsTotal = 0;

        // Capture values for lambda
        var offsets = ctx.Offsets;
        var resampledRefDose = ctx.ResampledRefDose;
        var comparedMinCol = ctx.ComparedMinCol;
        var comparedMaxCol = ctx.ComparedMaxCol;
        var threshDose = ctx.ThreshDose;
        var maxRefDose = ctx.MaxRefDose;
        var mx = ctx.Mx;
        var my = ctx.My;
        var resampledSizeX = ctx.ResampledSizeX;
        var resampledSizeY = ctx.ResampledSizeY;
        var doseCriteriaSq = ctx.DoseCriteriaSq;
        var dtaCriteriaSq = ctx.DtaCriteriaSq;
        var isGlobal = parameters.Global;

        Parallel.For(0, compared.Rows, () => (0, 0), (yi, loopState, localCounters) =>
            {
                if (comparedMinCol[yi] > comparedMaxCol[yi]) return localCounters;

                int rowOffset = yi * compared.Cols;
                int startXi = comparedMinCol[yi];
                int endXi = comparedMaxCol[yi];

                for (int xi = startXi; xi <= endXi; xi++)
                {
                    var comparedDose = compared.GetValue(xi, yi);

                    if (comparedDose < threshDose)
                        continue;

                    localCounters.Item2++;
                    double minGammaSquared = double.NaN;

                    foreach (var offset in offsets)
                    {
                        // Early termination: offsets are sorted by distance
                        double distComponent = offset.DistSquared / dtaCriteriaSq;
                        //if (!double.IsNaN(minGammaSquared) && distComponent >= minGammaSquared)
                        //    break;

                        var xiRef = offset.XIndexOffset + xi * mx;
                        var yiRef = offset.YIndexOffset + yi * my;

                        if (xiRef < 0 || yiRef < 0 || xiRef > resampledSizeX - 1 || yiRef > resampledSizeY - 1)
                            continue;

                        var refDose = resampledRefDose[yiRef, xiRef];

                        double doseDifference;
                        if (isGlobal)
                        {
                            if (maxRefDose == 0) continue;
                            doseDifference = 100 * (comparedDose - refDose) / maxRefDose;
                        }
                        else
                        {
                            if (refDose == 0) continue;
                            doseDifference = 100 * (comparedDose - refDose) / refDose;
                        }

                        var gammaSq = (doseDifference * doseDifference) / doseCriteriaSq + distComponent;

                        if (double.IsNaN(minGammaSquared) || gammaSq < minGammaSquared)
                            minGammaSquared = gammaSq;
                    }

                    if (minGammaSquared <= 1)
                        localCounters.Item1++;

                    gammaGrid.Data[rowOffset + xi] = (float)Math.Sqrt(minGammaSquared);
                }

                return localCounters;
            },
            (finalCounters) =>
            {
                Interlocked.Add(ref numPass, finalCounters.Item1);
                Interlocked.Add(ref ptsTotal, finalCounters.Item2);
            });

        var fracPass = ptsTotal > 0 ? (double)numPass / ptsTotal : 0.0;
        return (fracPass, gammaGrid);
    }

    /// <summary>
    /// Pre-compute offset positions including distances within the search radius.
    /// Offsets are sorted by distance for early termination optimization.
    /// </summary>
    private static Offset[] GetOffsets(double xRes, double yRes, double searchRadius)
    {
        var nX = (int)(2 * searchRadius / xRes) + 1;
        var nY = (int)(2 * searchRadius / yRes) + 1;
        var searchRadiusSq = searchRadius * searchRadius;

        var result = new List<Offset>();

        for (int i = -nX / 2; i <= nX / 2; i++)
        {
            for (int j = -nY / 2; j <= nY / 2; j++)
            {
                var distSq = (i * xRes) * (i * xRes) + (j * yRes) * (j * yRes);
                if (distSq <= searchRadiusSq)
                    result.Add(new Offset(i, j, distSq));
            }
        }

        return result.OrderBy(x => x.DistSquared).ToArray();
    }
}