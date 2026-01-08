using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Gamma;

public class GammaCalculator2D
{
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

        var resampledX = Enumerable.Range(0, nX).Select(i => compared.XMin + i * xSearchRes).ToArray();
        var resampledY = Enumerable.Range(0, nY).Select(i => compared.YMin + i * ySearchRes).ToArray();

        var resampledRefDose = new double[nY, nX];
        for (int i = 0; i < nX; i++)
        {
            for (int j = 0; j < nY; j++)
            {
                resampledRefDose[j, i] = reference.Interpolate(resampledX[i], resampledY[j]);
            }
        }

        var offsets = GetOffsets(xSearchRes, ySearchRes, searchRadMm);

        var gammaMap = new float[compared.Rows, compared.Cols];
        var xGamma = Enumerable.Range(0, compared.Cols).Select(i => compared.XMin + i * compared.XRes).ToList();
        var yGamma = Enumerable.Range(0, compared.Rows).Select(i => compared.YMin + i * compared.YRes).ToList();

        var maxRefDose = Math.Max(compared.Max(), reference.Max());
        var threshDose = compared.Max() * (parameters.ThresholdPercent / 100);

        int numPass = 0;
        int ptsTotal = 0;

        var failedPoints = new List<PointData>();

        for (int yi = 0; yi < compared.Rows; yi++)
        {
            for (int xi = 0; xi < compared.Cols; xi++)
            {
                var x = compared.GetX(xi);
                var y = compared.GetY(yi);

                gammaMap[yi, xi] = -1;

                if (!compared.Contains(x, y))
                    continue;

                var comparedDose = compared.GetValue(xi, yi);

                if (comparedDose < threshDose)
                    continue;

                ptsTotal++;
                double minGammaSquared = double.NaN;

                foreach (var offset in offsets)
                {
                    var xiRef = offset.XIndexOffset + xi * mx;
                    var yiRef = offset.YIndexOffset + yi * my;

                    if (xiRef < 0 || yiRef < 0 || xiRef > resampledX.Length - 1 || yiRef > resampledY.Length - 1)
                        continue;

                    var refDose = resampledRefDose[yiRef, xiRef];
                    var gammaSq = GammaSquared(comparedDose,
                        refDose,
                        maxRefDose,
                        offset.DistSquared,
                        parameters.DoseTolPercent,
                        parameters.DtaTolMm,
                        parameters.Global);
                    if (double.IsNaN(minGammaSquared) || gammaSq <= minGammaSquared)
                        minGammaSquared = gammaSq;
                }

                if (minGammaSquared <= 1)
                {
                    numPass++;
                }
                else
                {
                    failedPoints.Add(new(x, y, Math.Sqrt(minGammaSquared)));
                }

                gammaMap[yi, xi] = (float)Math.Sqrt(minGammaSquared);
            }
        }

        return new GammaResult2D(parameters, (double)numPass / ptsTotal, xGamma, yGamma, gammaMap);
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

        var result = new List<Offset>();

        for (int i = -nX / 2; i <= nX / 2; i++)
        {
            for (int j = -nY / 2; j <= nY / 2; j++)
            {
                var distSq = Math.Pow(i * xRes, 2) + Math.Pow(j * yRes, 2);
                if (distSq <= searchRadius * searchRadius)
                    result.Add(new Offset(i, j, distSq));
            }
        }

        return result.OrderBy(x => x.DistSquared).ToArray();
    }

    /// <summary>
    /// Returns the Gamma (note upper case) value given two doses/locations in the dose/ref dose profiles.
    /// </summary>
    /// <param name="dose">The dose value in the measured profile</param>
    /// <param name="doseRef">The dose value in the reference profile</param>
    /// <param name="doseRefMax">The maximum dose value in the reference profile</param>
    /// <param name="distSq"></param>
    /// <param name="doseCriteriaPercent">The dose difference criteria (in %) e.g 3%</param>
    /// <param name="dtaCriteriaMm">The DTA criteria (in %)</param>
    /// <param name="global">Whether to use global normalisation</param>
    /// <returns></returns>
    private double GammaSquared(double dose,
        double doseRef,
        double doseRefMax,
        double distSq,
        double doseCriteriaPercent,
        double dtaCriteriaMm, bool global)
    {
        var doseDifference = global ? 100 * (dose - doseRef) / doseRefMax : 100 * (dose - doseRef) / doseRef;

        return Math.Pow(doseDifference, 2) / Math.Pow(doseCriteriaPercent, 2) +
               distSq / Math.Pow(dtaCriteriaMm, 2);
    }
}