namespace TrajectoryLogReader.Fluence;

public interface IGrid<T>
{
    /// <summary>
    /// The number of cols
    /// </summary>
    int Cols { get; }

    /// <summary>
    /// The number of rows
    /// </summary>
    int Rows { get; }

    /// <summary>
    /// The size of each pixel in X
    /// </summary>
    double XRes { get; }

    /// <summary>
    /// The size of each pixel in Y
    /// </summary>
    double YRes { get; }

    /// <summary>
    /// The grid dimensions
    /// </summary>
    Rect Bounds { get; }

    /// <summary>
    /// The max value
    /// </summary>
    /// <returns></returns>
    T Max();

    /// <summary>
    /// Interpolate data at <paramref name="x"/> and <paramref name="y"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="valIfNotFound"></param>
    /// <returns></returns>
    T Interpolate(double x, double y, T valIfNotFound = default(T));

    /// <summary>
    /// Returns the data at <paramref name="row"/> and <paramref name="col"/> 
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    T GetData(int row, int col);

    /// <summary>
    /// Returns the x value at col <paramref name="col"/>
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    double GetX(int col);

    /// <summary>
    /// Returns the y value at row <paramref name="row"/>
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    double GetY(int row);

    /// <summary>
    /// Returns the data as a flat array row by row
    /// </summary>
    /// <returns></returns>
    T[] Flatten();
}