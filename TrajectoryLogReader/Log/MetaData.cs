namespace TrajectoryLogReader.Log
{
    public class MetaData
    {
        /// <summary>
        /// Patient URN/ID
        /// </summary>
        public string PatientId { get; internal set; } = string.Empty;

        /// <summary>
        /// The name of the plan
        /// </summary>
        public string PlanName { get; internal set; } = string.Empty;

        /// <summary>
        /// DICOM plan UID
        /// </summary>
        public string PlanUID { get; internal set; } = string.Empty;

        /// <summary>
        /// The SOP Instance UID of the plan.
        /// </summary>
        public string SOPInstanceUID { get; internal set; } = string.Empty;

        /// <summary>
        /// Total MU planned for the beam.
        /// </summary>
        public double MUPlanned { get; internal set; }

        /// <summary>
        /// Total MU remaining to be delivered (if any)
        /// </summary>
        public double MURemaining { get; internal set; }

        /// <summary>
        /// String representation of the beam energy
        /// </summary>
        public string Energy { get; internal set; } = string.Empty;

        /// <summary>
        /// The name of the beam that the log-file was recorded for.
        /// </summary>
        public string BeamName { get; internal set; } = string.Empty;
    }
}