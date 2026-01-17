namespace TrajectoryLogReader.Log
{
    public enum AxisScale
    {
        /// <summary>
        /// Used for setting default axis
        /// </summary>
        Default = 0,

        /// <summary>
        /// Varian native
        /// </summary>
        MachineScale = 1,

        /// <summary>
        /// Varian's modified IEC 61217 coordinate system
        /// </summary>
        ModifiedIEC61217 = 2,

        /// <summary>
        /// Varian native w isocentric couch
        /// </summary>
        MachineScaleIsocentric = 3,

        /// <summary>
        /// True IEC 61217 coordinate system (international standard)
        /// </summary>
        IEC61217 = 4
    }
}