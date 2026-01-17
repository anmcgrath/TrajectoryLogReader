namespace TrajectoryLogReader.Log
{
    public class AnonymizationOptions
    {
        /// <summary>
        /// The value to set for the Patient ID.
        /// </summary>
        public string PatientId { get; set; } = "Anonymized";

        /// <summary>
        /// The value to set for the Plan Name.
        /// </summary>
        public string PlanName { get; set; } = "Anonymized";

        /// <summary>
        /// The value to set for the Plan UID.
        /// </summary>
        public string PlanUID { get; set; } = "Anonymized";

        /// <summary>
        /// The value to set for the SOP Instance UID.
        /// </summary>
        public string SOPInstanceUID { get; set; } = "Anonymized";

        /// <summary>
        /// The value to set for the Beam Name.
        /// </summary>
        public string BeamName { get; set; } = "Anonymized";

        /// <summary>
        /// The value to set for the File Path.
        /// </summary>
        public string FilePath { get; set; } = "Anonymized";

        /// <summary>
        /// Set the subBeam name using sub-beam sequence number
        /// </summary>
        public Func<int, string> SubBeamNameSelector { get; set; } = i => $"Beam {i + 1}";
    }
}