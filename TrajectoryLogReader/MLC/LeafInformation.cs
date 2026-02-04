namespace TrajectoryLogReader.MLC
{
    public struct LeafInformation
    {
        /// <summary>
        /// Returns the Y position (in mm) of the centre of the leaf.
        /// Position increases towards the gun and is zero at the machine iso.
        /// </summary>
        public double YInMm { get; }

        /// <summary>
        /// The width of the MLC leaf in mm.
        /// </summary>
        public double WidthInMm { get; }

        public LeafInformation(double yInMm, double widthInMm)
        {
            YInMm = yInMm;
            WidthInMm = widthInMm;
        }
    }
}