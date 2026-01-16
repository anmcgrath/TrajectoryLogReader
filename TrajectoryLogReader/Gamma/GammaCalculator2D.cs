using System.Diagnostics;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Gamma;

/// <summary>
/// Calculates 2D Gamma index between two fluence maps.
/// </summary>
public class GammaCalculator2D
{
    /// <summary>
    /// Perform a gamma comparison between two field fluences.
    /// </summary>
    /// <param name="parameters">Gamma parameters.</param>
    /// <param name="reference">Reference fluence.</param>
    /// <param name="compared">Compared fluence.</param>
    /// <returns>Gamma result.</returns>
    public GammaResult2D Calculate(GammaParameters2D parameters, FieldFluence reference, FieldFluence compared)
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
    public GammaResult2D Calculate(GammaParameters2D parameters, IGrid<float> reference, IGrid<float> compared)
    {
        var searchRadMm = parameters.SearchRadius ?? parameters.DtaTolMm * 2;

        var xSearchRes = parameters.DtaTolMm / parameters.SamplingRate;
        var ySearchRes = parameters.DtaTolMm / parameters.SamplingRate;

        // resample the reference grid before we start so we avoid doing costly interpolations
        // the resampled dose grid has a resolution of (close to) xSearchRes/ySearchRes
        // the resampled grid is designed to have points that exactly correspond to the dose points in the 
        // compared dose grid.
        // mx or my = the number of spaces between original dose points
        // e.g [ ]   x    x   [ ] has 3 spaces with 2 additional points at x
        var mx = (int)Math.Ceiling(compared.XRes / xSearchRes);
        var my = (int)Math.Ceiling(compared.YRes / ySearchRes);

        xSearchRes = compared.XRes / mx;
        ySearchRes = compared.YRes / my;

        var resampledSizeX = (mx - 1) * (compared.Cols - 1) + compared.Cols;
        var resampledSizeY = (my - 1) * (compared.Rows - 1) + compared.Rows;

        var maxRefDose = Math.Max(compared.Max(), reference.Max());
        var threshDose = compared.Max() * (parameters.ThresholdPercent / 100);

        var resampledX = new double[resampledSizeX];
        for (int i = 0; i < resampledSizeX; i++)
        {
            resampledX[i] = compared.XMin + i * xSearchRes;
        }

        var resampledY = new double[resampledSizeY];
        for (int i = 0; i < resampledSizeY; i++)
        {
            resampledY[i] = compared.YMin + i * ySearchRes;
        }

        var resampledRefDose = new double[resampledSizeY, resampledSizeX];

        // Pre-calculate the start/end column for each row in the resampled grid
        // This optimization limits the costly interpolation to only those points within the reference grid
        // that are relevant for the comparison (i.e. near points where compared dose > threshold).
        var minCol = new int[resampledSizeY];
        for (int k = 0; k < resampledSizeY; k++) minCol[k] = int.MaxValue;
        var maxCol = new int[resampledSizeY];
        for (int k = 0; k < resampledSizeY; k++) maxCol[k] = int.MinValue;

        // Also pre-calculate the start/end column for the compared grid rows
        var comparedMinCol = new int[compared.Rows];
        for (int k = 0; k < compared.Rows; k++) comparedMinCol[k] = int.MaxValue;
        var comparedMaxCol = new int[compared.Rows];
        for (int k = 0; k < compared.Rows; k++) comparedMaxCol[k] = int.MinValue;

        var nX = (int)(2 * searchRadMm / xSearchRes) + 1;
        var nY = (int)(2 * searchRadMm / ySearchRes) + 1;
        var marginX = nX / 2;
        var marginY = nY / 2;

        for (int yi = 0; yi < compared.Rows; yi++)
        {
            int minXi = -1, maxXi = -1;

            // Find min/max xi in this row where dose > threshold
            for (int xi = 0; xi < compared.Cols; xi++)
            {
                if (compared.GetValue(xi, yi) >= threshDose)
                {
                    if (minXi == -1) minXi = xi;
                    maxXi = xi;
                }
            }

            if (minXi == -1) continue;

            // Store bounds for the compared grid loop
            comparedMinCol[yi] = minXi;
            comparedMaxCol[yi] = maxXi;

            // Map to resampled coordinates
            int cY = yi * my;
            int startX = minXi * mx;
            int endX = maxXi * mx;

            // Apply search radius margins
            int startJ = Math.Max(0, cY - marginY);
            int endJ = Math.Min(resampledSizeY - 1, cY + marginY);

            int rangeMinX = startX - marginX;
            int rangeMaxX = endX + marginX;

            for (int j = startJ; j <= endJ; j++)
            {
                if (rangeMinX < minCol[j]) minCol[j] = rangeMinX;
                if (rangeMaxX > maxCol[j]) maxCol[j] = rangeMaxX;
            }
        }

        // Parallelize resampling using the pre-calculated bounds
        Parallel.For(0, resampledSizeY, j =>
        {
            if (minCol[j] > maxCol[j]) return;

            int startI = Math.Max(0, minCol[j]);
            int endI = Math.Min(resampledSizeX - 1, maxCol[j]);

            for (int i = startI; i <= endI; i++)
            {
                resampledRefDose[j, i] = reference.Interpolate(resampledX[i], resampledY[j]);
            }
        });

        var offsets = GetOffsets(xSearchRes, ySearchRes, searchRadMm);

        var gammaGrid = new GridF(compared.XMax - compared.XMin, compared.YMax - compared.YMin, compared.Cols,
            compared.Rows);

        // Initialize gamma grid with -1
        for (int i = 0; i < gammaGrid.Data.Length; i++)
        {
            gammaGrid.Data[i] = -1;
        }

        // Precompute constants
        double doseCriteriaSq = parameters.DoseTolPercent * parameters.DoseTolPercent;
        double dtaCriteriaSq = parameters.DtaTolMm * parameters.DtaTolMm;

        int numPass = 0;
        int ptsTotal = 0;

        // Parallelize Gamma calculation
        Parallel.For(0, compared.Rows, () => (0, 0), (yi, loopState, localCounters) =>
            {
                if (comparedMinCol[yi] > comparedMaxCol[yi]) return localCounters;

                int rowOffset = yi * compared.Cols;
                int startXi = comparedMinCol[yi];
                int endXi = comparedMaxCol[yi];

                for (int xi = startXi; xi <= endXi; xi++)
                {
                    var x = compared.GetX(xi);
                    var y = compared.GetY(yi);

                    if (!compared.Contains(x, y))
                        continue;

                    var comparedDose = compared.GetValue(xi, yi);

                    if (comparedDose < threshDose)
                        continue;

                    localCounters.Item2++; // ptsTotal++
                    double minGammaSquared = double.NaN;

                    foreach (var offset in offsets)
                    {
                        var xiRef = offset.XIndexOffset + xi * mx;
                        var yiRef = offset.YIndexOffset + yi * my;

                        if (xiRef < 0 || yiRef < 0 || xiRef > resampledSizeX - 1 || yiRef > resampledSizeY - 1)
                            continue;

                        var refDose = resampledRefDose[yiRef, xiRef];

                        // Inline GammaSquared logic
                        double doseDifference;
                        if (parameters.Global)
                        {
                            // Global: normalize to max reference dose
                            if (maxRefDose == 0)
                                continue; // Skip if max dose is zero to avoid division by zero
                            doseDifference = 100 * (comparedDose - refDose) / maxRefDose;
                        }
                        else
                        {
                            // Local: normalize to reference dose at this point
                            if (refDose == 0)
                                continue; // Skip zero-dose reference points to avoid division by zero
                            doseDifference = 100 * (comparedDose - refDose) / refDose;
                        }

                        var gammaSq = (doseDifference * doseDifference) / doseCriteriaSq +
                                      offset.DistSquared / dtaCriteriaSq;

                        if (double.IsNaN(minGammaSquared) || gammaSq <= minGammaSquared)
                            minGammaSquared = gammaSq;
                    }

                    if (minGammaSquared <= 1)
                    {
                        localCounters.Item1++; // numPass++
                    }

                    gammaGrid.Data[rowOffset + xi] = (float)Math.Sqrt(minGammaSquared);
                }

                return localCounters;
            },
            (finalCounters) =>
            {
                Interlocked.Add(ref numPass, finalCounters.Item1);
                Interlocked.Add(ref ptsTotal, finalCounters.Item2);
            });

        // Handle case where no points were above threshold
        var fracPass = ptsTotal > 0 ? (double)numPass / ptsTotal : 0.0;
        return new GammaResult2D(parameters, fracPass, gammaGrid);
    }

    /// <summary>
    /// Pre-compute offset positions including distances within the <paramref name="searchRadius"/> given.
    /// </summary>
    /// <param name="xRes"></param>
    /// <param name="yRes"></param>
    /// <param name="searchRadius"></param>
    /// <returns></returns>
    private Offset[] GetOffsets(double xRes, double yRes, double searchRadius)
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