namespace TrajectoryLogReader.MLC
{
    public interface IMLCModel
    {
        LeafInformation GetLeafInformation(int leafIndex);
    }
}