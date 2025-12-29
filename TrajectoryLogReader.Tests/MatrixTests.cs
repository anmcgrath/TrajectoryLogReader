using Shouldly;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class MatrixTests
{
    [Test]
    public void Constructor_WithValidArray_CreatesMatrix()
    {
        var values = new double[,] { { 1, 2 }, { 3, 4 } };
        var matrix = new Matrix(values);

        matrix[0, 0].ShouldBe(1);
        matrix[0, 1].ShouldBe(2);
        matrix[1, 0].ShouldBe(3);
        matrix[1, 1].ShouldBe(4);
    }

    [Test]
    public void Constructor_WithInvalidDimensions_ThrowsArgumentException()
    {
        var values = new double[,] { { 1, 2, 3 }, { 4, 5, 6 } };
        Should.Throw<ArgumentException>(() => new Matrix(values));
    }

    [Test]
    public void Rotation_CreatesCorrectRotationMatrix()
    {
        var angle = Math.PI / 2; // 90 degrees
        var matrix = Matrix.Rotation(angle);

        // cos(90) = 0, sin(90) = 1
        // [ 0 -1 ]
        // [ 1  0 ]
        
        matrix[0, 0].ShouldBe(0, 1e-10);
        matrix[0, 1].ShouldBe(-1, 1e-10);
        matrix[1, 0].ShouldBe(1, 1e-10);
        matrix[1, 1].ShouldBe(0, 1e-10);
    }

    [Test]
    public void Scaling_CreatesCorrectScalingMatrix()
    {
        var matrix = Matrix.Scaling(2, 3);

        matrix[0, 0].ShouldBe(2);
        matrix[0, 1].ShouldBe(0);
        matrix[1, 0].ShouldBe(0);
        matrix[1, 1].ShouldBe(3);
    }

    [Test]
    public void Multiply_Point_ReturnsCorrectResult()
    {
        var matrix = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var point = new Point(5, 6);

        // [ 1 2 ] * [ 5 ] = [ 1*5 + 2*6 ] = [ 17 ]
        // [ 3 4 ]   [ 6 ]   [ 3*5 + 4*6 ]   [ 39 ]

        var result = matrix.Multiply(point);

        result.X.ShouldBe(17);
        result.Y.ShouldBe(39);
    }

    [Test]
    public void Multiply_Matrix_ReturnsCorrectResult()
    {
        var m1 = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var m2 = new Matrix(new double[,] { { 5, 6 }, { 7, 8 } });

        // [ 1 2 ] * [ 5 6 ] = [ 1*5+2*7  1*6+2*8 ] = [ 19 22 ]
        // [ 3 4 ]   [ 7 8 ]   [ 3*5+4*7  3*6+4*8 ]   [ 43 50 ]

        var result = m1.Multiply(m2);

        result[0, 0].ShouldBe(19);
        result[0, 1].ShouldBe(22);
        result[1, 0].ShouldBe(43);
        result[1, 1].ShouldBe(50);
    }

    [Test]
    public void Operator_Multiply_Point_WorksAsMethod()
    {
        var matrix = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var point = new Point(5, 6);

        var result = matrix * point;

        result.X.ShouldBe(17);
        result.Y.ShouldBe(39);
    }

    [Test]
    public void Operator_Multiply_Matrix_WorksAsMethod()
    {
        var m1 = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var m2 = new Matrix(new double[,] { { 5, 6 }, { 7, 8 } });

        var result = m1 * m2;

        result[0, 0].ShouldBe(19);
        result[0, 1].ShouldBe(22);
        result[1, 0].ShouldBe(43);
        result[1, 1].ShouldBe(50);
    }
    
    [Test]
    public void Indexer_AccessesCorrectElements()
    {
        var matrix = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        
        matrix[0, 0].ShouldBe(1);
        matrix[0, 1].ShouldBe(2);
        matrix[1, 0].ShouldBe(3);
        matrix[1, 1].ShouldBe(4);
    }

    [Test]
    public void Indexer_ThrowsOnInvalidIndices()
    {
        var matrix = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        
        Should.Throw<IndexOutOfRangeException>(() => { var x = matrix[2, 0]; });
        Should.Throw<IndexOutOfRangeException>(() => { var x = matrix[0, 2]; });
    }
}
