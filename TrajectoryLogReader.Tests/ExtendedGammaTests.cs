using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Fluence;
using TrajectoryLogReader.Gamma;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class PhysicistGammaTests
{
    // Helper to create a grid with a specific pattern
    private GridF CreateGrid(int rows, int cols, Func<int, int, float> valueFunc, double res = 1.0)
    {
        var grid = new GridF(new Rect(0, 0, cols * res, rows * res), cols, rows);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                grid.SetData(c, r, valueFunc(c, r));
            }
        }

        return grid;
    }

    [Test]
    public void Gamma_ShiftedStepFunction_PassesWithDTA()
    {
        // Scenario: A sharp step function (50 -> 100) shifted by 2mm.
        // We use 50 instead of 0 to ensure values are above the 10% threshold.
        // If we only check dose diff (3%), it fails (100 vs 50 is huge).
        // If we check DTA (3mm), it should pass because the "50" is only 2mm away.

        // Resolution 1mm
        var refGrid = CreateGrid(10, 10, (c, r) => c < 5 ? 50f : 100f);
        var evalGrid = CreateGrid(10, 10, (c, r) => c < 7 ? 50f : 100f); // Shifted by 2 pixels (2mm)

        // Gamma: 3% / 3mm
        var params33 = new GammaParameters2D(3, 3, true, 10);
        var result = GammaCalculator2D.Calculate(params33, refGrid, evalGrid);
        result.FracPass.ShouldBe(1.0, "All points should pass due to DTA");

        var params31 = new GammaParameters2D(1, 3, true, 10);

        var resultStrict = GammaCalculator2D.Calculate(params31, refGrid, evalGrid);

        resultStrict.FracPass.ShouldBeLessThan(1.0, "Points in the gap should fail strict DTA");
    }

    [Test]
    public void Gamma_DoseDifference_Fails_When_Distance_Is_Too_Large()
    {
        var refGrid = CreateGrid(5, 5, (_, _) => 100f);
        var evalGrid = CreateGrid(5, 5, (_, _) => 105f);

        var params33 = new GammaParameters2D(3, 3, true, 10);
        var result = GammaCalculator2D.Calculate(params33, refGrid, evalGrid);

        result.FracPass.ShouldBe(0.0, "All points should fail dose difference");
        result.Grid.Data[0].ShouldBeGreaterThan(1.0f);
    }

    [Test]
    public void Gamma_Global_vs_Local_Normalization()
    {
        var refGrid = CreateGrid(5, 5, (_, _) => 10f);
        // Inject a high dose point to set global max
        refGrid.SetData(0, 0, 100f);

        var evalGrid = CreateGrid(5, 5, (_, _) => 11f); // 10% error locally
        evalGrid.SetData(0, 0, 100f); // Match the max

        // Global
        var paramsGlobal = new GammaParameters2D(3, 3, true, 10);
        var resGlobal = GammaCalculator2D.Calculate(paramsGlobal, refGrid, evalGrid);

        // Local
        var paramsLocal = new GammaParameters2D(3, 3, false, 10);
        var resLocal = GammaCalculator2D.Calculate(paramsLocal, refGrid, evalGrid);

        resGlobal.FracPass.ShouldBe(1.0, "Global norm should mask low dose error");
        resLocal.FracPass.ShouldBeLessThan(1.0, "Local norm should catch low dose error");
    }

    [Test]
    public void Gamma_Threshold_Ignores_Low_Dose_Points()
    {
        var refGrid = CreateGrid(5, 5, (_, _) => 1f);
        refGrid.SetData(0, 0, 100f);

        var evalGrid = CreateGrid(5, 5, (_, _) => 1f);
        evalGrid.SetData(0, 0, 100f);

        var paramsThresh = new GammaParameters2D(3, 3, true, 10); // 10% threshold
        var result = GammaCalculator2D.Calculate(paramsThresh, refGrid, evalGrid);
        evalGrid.SetData(0, 0, 110f); // 10% diff, fails 3%

        var resultFail = GammaCalculator2D.Calculate(paramsThresh, refGrid, evalGrid);

        resultFail.FracPass.ShouldBe(0.0, "Only high dose points should be evaluated");
    }
}