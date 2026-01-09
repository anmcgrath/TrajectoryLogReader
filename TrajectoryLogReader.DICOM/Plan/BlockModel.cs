namespace TrajectoryLogReader.DICOM.Plan;

/// <summary>
/// Represents a block in a DICOM plan.
/// </summary>
public class BlockModel
{
    /// <summary>
    /// The Block Name.
    /// </summary>
    public string BlockName { get; set; }
    
    /// <summary>
    /// The Block Type.
    /// </summary>
    public string BlockType { get; set; }
    
    /// <summary>
    /// The Block Tray ID.
    /// </summary>
    public string BlockTrayID { get; set; }
    
    /// <summary>
    /// The Block Number.
    /// </summary>
    public int BlockNumber { get; set; }
    
    /// <summary>
    /// The Block Data (coordinates).
    /// </summary>
    public string BlockData { get; set; }
}
