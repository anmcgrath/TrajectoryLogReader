namespace TrajectoryLogReader.MLC
{
    public class Millenium120MLC : IMLCModel
    {
        public LeafInformation GetLeafInformation(int leafIndex)
        {
            var y0 = (float)(-20 * 0.5 - 10 * 1.0 +
                             0.5); // y of centre of first mlc leaf (index 0, which is towards y1)
            if (leafIndex <= 9)
                return new LeafInformation((leafIndex * 1 + y0) * 10f, 10f);

            if (leafIndex <= 49)
                return new LeafInformation((float)(y0 + 9.5 + (leafIndex - 10) * 0.5 + 0.25) * 10, 5f);

            // between 50 -> 59
            return new LeafInformation((float)(10 + 0.5 + (leafIndex - 50) * 1.0) * 10, 10f);
        }

        public int GetNumberOfLeafPairs()
        {
            return 60;
        }
    }
}