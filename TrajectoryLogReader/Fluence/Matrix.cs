namespace TrajectoryLogReader.Fluence;

internal class Matrix
{
    // Flatten the array to fields for faster access and to avoid array bounds checks
    private readonly double _m00, _m01, _m10, _m11;

    public Matrix(double[,] values)
    {
        if (values.GetLength(0) != 2 || values.GetLength(1) != 2)
            throw new ArgumentException("Matrix must be 2x2");

        _m00 = values[0, 0];
        _m01 = values[0, 1];
        _m10 = values[1, 0];
        _m11 = values[1, 1];
    }

    // Private constructor for internal optimizations to avoid array allocation
    private Matrix(double m00, double m01, double m10, double m11)
    {
        _m00 = m00;
        _m01 = m01;
        _m10 = m10;
        _m11 = m11;
    }

    /// <summary>
    /// Creates a rotation matrix
    /// </summary>
    /// <param name="angleRadians">Rotation angle in radians (counter-clockwise)</param>
    public static Matrix Rotation(double angleRadians)
    {
        double cos = Math.Cos(angleRadians);
        double sin = Math.Sin(angleRadians);

        return new Matrix(cos, -sin, sin, cos);
    }

    /// <summary>
    /// Creates a scaling matrix
    /// </summary>
    /// <param name="scaleX">Scale factor in X direction</param>
    /// <param name="scaleY">Scale factor in Y direction</param>
    public static Matrix Scaling(double scaleX, double scaleY)
    {
        return new Matrix(scaleX, 0, 0, scaleY);
    }

    /// <summary>
    /// Multiplies this matrix with a point (left matrix multiplication)
    /// </summary>
    public Point Multiply(Point point)
    {
        double x = _m00 * point.X + _m01 * point.Y;
        double y = _m10 * point.X + _m11 * point.Y;

        return new Point(x, y);
    }

    /// <summary>
    /// Multiplies this matrix with another matrix
    /// </summary>
    public Matrix Multiply(Matrix other)
    {
        // Unrolled multiplication for better performance
        return new Matrix(
            _m00 * other._m00 + _m01 * other._m10,
            _m00 * other._m01 + _m01 * other._m11,
            _m10 * other._m00 + _m11 * other._m10,
            _m10 * other._m01 + _m11 * other._m11
        );
    }

    /// <summary>
    /// Operator overload for matrix-point multiplication
    /// </summary>
    public static Point operator *(Matrix matrix, Point point)
    {
        return matrix.Multiply(point);
    }

    /// <summary>
    /// Operator overload for matrix-matrix multiplication
    /// </summary>
    public static Matrix operator *(Matrix left, Matrix right)
    {
        return left.Multiply(right);
    }

    /// <summary>
    /// Gets the value at the specified row and column (0-indexed)
    /// </summary>
    public double this[int row, int col]
    {
        get
        {
            if (row == 0)
            {
                if (col == 0) return _m00;
                if (col == 1) return _m01;
            }
            else if (row == 1)
            {
                if (col == 0) return _m10;
                if (col == 1) return _m11;
            }
            throw new IndexOutOfRangeException();
        }
    }
}
