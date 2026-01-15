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
        var searchRadMm = parameters.DtaTolMm * 1.5;

        var xSearchRes = parameters.DtaTolMm / 10;
        var ySearchRes = parameters.DtaTolMm / 10;

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

        var nX = (mx - 1) * (compared.Cols - 1) + compared.Cols;
        var nY = (my - 1) * (compared.Rows - 1) + compared.Rows;

        var resampledX = new double[nX];
        for (int i = 0; i < nX; i++)
        {
            resampledX[i] = compared.XMin + i * xSearchRes;
        }

        var resampledY = new double[nY];
        for (int i = 0; i < nY; i++)
        {
            resampledY[i] = compared.YMin + i * ySearchRes;
        }

        var resampledRefDose = new double[nY, nX];

        // Parallelize resampling
        Parallel.For(0, nX, i =>
        {
            for (int j = 0; j < nY; j++)
            {
                resampledRefDose[j, i] = reference.Interpolate(resampledX[i], resampledY[j]);
            }
        });

        var offsets = GetOffsets(xSearchRes, ySearchRes, searchRadMm);

        var gammaGrid = new GridF(compared.XMax - compared.XMin, compared.YMax - compared.YMin, compared.Cols,
            compared.Rows);

        var maxRefDose = Math.Max(compared.Max(), reference.Max());
        var threshDose = compared.Max() * (parameters.ThresholdPercent / 100);

        // Precompute constants
        double doseCriteriaSq = parameters.DoseTolPercent * parameters.DoseTolPercent;
        double dtaCriteriaSq = parameters.DtaTolMm * parameters.DtaTolMm;

        int numPass = 0;
        int ptsTotal = 0;

        // Parallelize Gamma calculation
        Parallel.For(0, compared.Rows, () => (0, 0), (yi, loopState, localCounters) =>
        {
            int rowOffset = yi * compared.Cols;
            for (int xi = 0; xi < compared.Cols; xi++)
            {
                var x = compared.GetX(xi);
                var y = compared.GetY(yi);

                // Default to -1 (no calculation)
                gammaGrid.Data[rowOffset + xi] = -1;

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

                    if (xiRef < 0 || yiRef < 0 || xiRef > nX - 1 || yiRef > nY - 1)
                        continue;

                    var refDose = resampledRefDose[yiRef, xiRef];

                    // Inline GammaSquared logic
                    double doseDifference;
                    if (parameters.Global)
                    {
                        doseDifference = 100 * (comparedDose - refDose) / maxRefDose;
                    }
                    else
                    {
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

        return new GammaResult2D(parameters, (double)numPass / ptsTotal, gammaGrid);
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
