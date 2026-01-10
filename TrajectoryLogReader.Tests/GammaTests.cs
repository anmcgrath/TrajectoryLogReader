using Shouldly;
using TrajectoryLogReader.Fluence;
using TrajectoryLogReader.Gamma;

namespace TrajectoryLogReader.Tests;

public class GammaTests
{
    [Test]
    public void Gamma_With_Flat_Grids_Returns_Dose_Difference()
    {
        var g1 = new GridF(3, 3, 3, 3);
        var g2 = new GridF(3, 3, 3, 3);
        FillGrid(g1, 100);
        FillGrid(g2, 100.5f); // half a prcnt higher
        var calc = new GammaCalculator2D();
        var gParams = new GammaParameters2D(1, 1, true, 10);
        var res = calc
            .Calculate(gParams, new FluenceGridWrapper(g1), new FluenceGridWrapper(g2));
        res.FracPass.ShouldBe(1);

        var diff = 100 * (100.5 - 100) / 100.5;

        for (int i = 0; i < g1.Rows; i++)
        {
            for (int j = 0; j < g1.Cols; j++)
            {
                // GammaResult2D.Data is now flat
                res.Data[i * g1.Cols + j].ShouldBe((float)diff, 0.001);
            }
        }
    }

    private void FillGrid(GridF grid, float data)
    {
        for (int i = 0; i < grid.Rows; i++)
        {
            for (int j = 0; j < grid.Cols; j++)
            {
                grid.SetData(j, i, data);
            }
        }
    }
}