namespace TrajectoryLogReader.Log
{
    /// <summary>
    /// Defines the coordinate system used to interpret axis values in a trajectory log.
    /// In practice this controls sign conventions for rotations/translations and how couch
    /// axes are referenced (for example, isocentric vs. table coordinates).
    /// </summary>
    public enum AxisScale
    {
        /// <summary>
        /// Uses the scale declared in the log header. This is the safest option when you want
        /// to preserve the machine-native meaning of the data.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Varian machine-native scale as recorded by the control system.
        /// This often differs from IEC 61217 in sign and reference definitions.
        /// </summary>
        MachineScale = 1,

        /// <summary>
        /// Varian's modified IEC 61217 scale. This is commonly used by clinical systems and may
        /// be closer to treatment-planning-system conventions than pure machine scale.
        /// </summary>
        ModifiedIEC61217 = 2,

        /// <summary>
        /// Varian machine-native scale with the couch axes referenced isocentrically.
        /// This can be useful when comparing to isocenter-based coordinate expectations.
        /// </summary>
        MachineScaleIsocentric = 3,

        /// <summary>
        /// IEC 61217 (international standard) coordinate system.
        /// </summary>
        IEC61217 = 4
    }
}
