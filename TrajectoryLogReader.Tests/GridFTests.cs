using NUnit.Framework;
using Shouldly;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class GridFTests
{
    [Test]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var width = 10.0;
        var height = 20.0;
        var cols = 5;
        var rows = 4;

        var grid = new GridF(width, height, cols, rows);

        grid.Width.ShouldBe(width);
        grid.Height.ShouldBe(height);
        grid.Cols.ShouldBe(cols);
        grid.Rows.ShouldBe(rows);
        grid.XRes.ShouldBe(2.0); // 10 / 5
        grid.YRes.ShouldBe(5.0); // 20 / 4
        grid.Data.Length.ShouldBe(cols * rows);

        // Bounds check
        grid.Bounds.X.ShouldBe(-5.0); // -10/2
        grid.Bounds.Y.ShouldBe(-10.0); // -20/2
        grid.Bounds.Width.ShouldBe(10.0);
        grid.Bounds.Height.ShouldBe(20.0);
    }

    [Test]
    public void Indexer_GetAndSet_WorksCorrectly()
    {
        var grid = new GridF(10, 10, 5, 5);

        // Set value at row 2, col 3
        grid[2, 3] = 42.5f;

        // Verify via indexer
        grid[2, 3].ShouldBe(42.5f);

        // Verify via flat array directly
        // Index should be row * Cols + col = 2 * 5 + 3 = 13
        grid.Data[13].ShouldBe(42.5f);
    }

    [Test]
    public void Add_WithSameDimensions_SumsValuesCorrectly()
    {
        // This tests the SIMD logic in Add
        int cols = 100;
        int rows = 100;
        var grid1 = new GridF(10, 10, cols, rows);
        var grid2 = new GridF(10, 10, cols, rows);

        // Fill grids with deterministic data
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                grid1[i, j] = 1.0f;
                grid2[i, j] = 2.0f;
            }
        }

        grid1.Add(grid2);

        // Verify all elements are sum
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                grid1[i, j].ShouldBe(3.0f);
            }
        }
    }

    [Test]
    public void Add_WithSmallGrid_SumsValuesCorrectly()
    {
        // Test edge case where grid is smaller than vector size or not multiple
        int cols = 3;
        int rows = 3;
        var grid1 = new GridF(10, 10, cols, rows);
        var grid2 = new GridF(10, 10, cols, rows);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                grid1[i, j] = (i * cols + j);
                grid2[i, j] = 1.0f;
            }
        }

        grid1.Add(grid2);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var expected = (i * cols + j) + 1.0f;
                grid1[i, j].ShouldBe(expected);
            }
        }
    }

    [Test]
    public void Add_WithDifferentDimensions_ThrowsArgumentException()
    {
        var grid1 = new GridF(10, 10, 10, 10);
        var grid2 = new GridF(10, 10, 5, 5);

        Should.Throw<ArgumentException>(() => grid1.Add(grid2));
    }

    [Test]
    public void GetX_GetY_ReturnCorrectPhysicalCoordinates()
    {
        // Width 10, Cols 5 -> XRes 2. Center 0. Bounds X -5.
        // Col 0: -5
        // Col 1: -3
        // Col 2: -1
        // Col 3: 1
        // Col 4: 3
        var grid = new GridF(10, 10, 5, 5);

        grid.GetX(0).ShouldBe(-5.0);
        grid.GetX(2).ShouldBe(-1.0);
        grid.GetX(4).ShouldBe(3.0);

        // Similar for Y
        grid.GetY(0).ShouldBe(-5.0);
    }

    [Test]
    public void Interpolate_ReturnsBilinearInterpolatedValue()
    {
        var grid = new GridF(10, 10, 2, 2);

        grid[0, 0] = 0;
        grid[0, 1] = 10;
        grid[1, 0] = 0;
        grid[1, 1] = 10;

        // Interpolate at (-2.5, -2.5) -> Middle of the square formed by these points

        grid.Interpolate(-2.5, -2.5).ShouldBe(5.0f);

        // Point exactly on grid point
        grid.Interpolate(-5, -5).ShouldBe(0f);
        grid.Interpolate(0, 0).ShouldBe(10f);
    }
}