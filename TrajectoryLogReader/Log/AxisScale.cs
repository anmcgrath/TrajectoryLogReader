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
        /// Varian IEC
        /// </summary>
        ModifiedIEC61217 = 2,

        /// <summary>
        /// Varian native w isocentric couch
        /// </summary>
        MachineScaleIsocentric = 3
    }
}