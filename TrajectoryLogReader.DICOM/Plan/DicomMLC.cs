using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.DICOM;

/// <summary>
/// Represents an MLC model derived from DICOM data.
/// </summary>
public class DicomMLC : IMLCModel
{
    private float[] _leafCentres;
    private float[] _leafWidths;
    private int _nLeafPairs;

    internal DicomMLC(float[] leafBoundaries)
    {
        _nLeafPairs = leafBoundaries.Length - 1;
        _leafCentres = new float[_nLeafPairs];
        _leafWidths = new float[_nLeafPairs];

        for (int i = 0; i < _nLeafPairs; i++)
        {
            _leafCentres[i] = (leafBoundaries[i] + leafBoundaries[i + 1]) / 2;
            _leafWidths[i] = leafBoundaries[i + 1] - leafBoundaries[i];
        }
    }

    /// <inheritdoc />
    public LeafInformation GetLeafInformation(int leafIndex)
    {
        return new LeafInformation(_leafCentres[leafIndex], _leafWidths[leafIndex]);
    }

    /// <inheritdoc />
    public int GetNumberOfLeafPairs()
    {
        return _nLeafPairs;
    }
}