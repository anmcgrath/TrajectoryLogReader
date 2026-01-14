namespace TrajectoryLogReader.MLC
{
    /// <summary>
    /// Represents the MLC leaf bank.
    /// </summary>
    public enum Bank
    {
        /// <summary>
        /// Bank B (Left/X1 side usually, index 0 in internal structures)
        /// </summary>
        B = 0,
        
        /// <summary>
        /// Bank A (Right/X2 side usually, index 1 in internal structures)
        /// </summary>
        A = 1
    }
}