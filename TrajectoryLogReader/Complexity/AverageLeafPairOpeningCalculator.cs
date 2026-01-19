using TrajectoryLogReader.Log;
using TrajectoryLogReader.Log.Snapshots;
using TrajectoryLogReader.Util;

namespace TrajectoryLogReader.Complexity;

/// <summary>
/// Calculates the Average Leaf Pair Opening (ALPO) from trajectory log data.
/// ALPO is a plan complexity metric that measures the average gap between
/// opposing MLC leaves for leaf pairs within the jaw opening.
/// </summary>
public static class AverageLeafPairOpeningCalculator
{
    /// <summary>
    /// Calculates the Average Leaf Pair Opening in centimeters for a collection of snapshots.
    /// Only includes leaf pairs whose center is within the Y jaw opening.
    /// </summary>
    /// <param name="snapshots">The snapshot collection to analyze.</param>
    /// <param name="options">Calculation options. If null, default options are used.</param>
    /// <returns>The average leaf pair opening in centimeters.</returns>
    public static double Calculate(SnapshotCollection snapshots, AverageLeafPairOpeningOptions? options = null)
    {
        options ??= new AverageLeafPairOpeningOptions();

        double totalOpening = 0;
        long totalLeafPairCount = 0;

        foreach (var snapshot in snapshots)
        {
            // Skip if MU is zero and we're not including zero MU snapshots
            if (!options.IncludeZeroMu)
            {
                var deltaMu = snapshot.DeltaMu.GetRecord(options.RecordType);
                if (deltaMu <= 0)
                    continue;
            }

            // Skip if beam is on hold and we're not including beam hold snapshots
            if (!options.IncludeBeamHold)
            {
                var beamHold = snapshot.BeamHold.GetRecord(options.RecordType);
                if (beamHold > 0)
                    continue;
            }

            var (snapshotOpening, leafCount) = CalculateForSnapshot(snapshot, options.RecordType);
            totalOpening += snapshotOpening;
            totalLeafPairCount += leafCount;
        }

        return totalLeafPairCount > 0 ? totalOpening / totalLeafPairCount : 0;
    }

    /// <summary>
    /// Calculates the total opening and leaf count for a single snapshot.
    /// </summary>
    private static (double totalOpening, int leafCount) CalculateForSnapshot(Snapshot snapshot, RecordType recordType)
    {
        var mlcModel = snapshot.MlcModel;
        var numLeafPairs = mlcModel.GetNumberOfLeafPairs();

        // Get jaw positions in IEC scale (cm)
        var y1 = snapshot.Y1.WithScale(AxisScale.IEC61217).GetRecord(recordType);
        var y2 = snapshot.Y2.WithScale(AxisScale.IEC61217).GetRecord(recordType);

        // Ensure y1 < y2 (y1 is toward floor/negative, y2 is toward gun/positive)
        if (y1 > y2)
            (y1, y2) = (y2, y1);

        // Convert to mm for comparison with leaf positions
        var y1Mm = y1 * 10;
        var y2Mm = y2 * 10;

        var mlcSnapshot = snapshot.MLC.WithScale(AxisScale.IEC61217);

        double totalOpening = 0;
        int includedLeafCount = 0;

        for (int leafIndex = 0; leafIndex < numLeafPairs; leafIndex++)
        {
            var leafInfo = mlcModel.GetLeafInformation(leafIndex);
            var leafCenterY = leafInfo.YInMm;

            // Check if leaf center is within the Y jaw opening
            if (leafCenterY < y1Mm || leafCenterY > y2Mm)
                continue;

            // Get leaf positions for both banks (in cm, IEC scale)
            var bankBPos = mlcSnapshot.GetLeaf(0, leafIndex).GetRecord(recordType);
            var bankAPos = mlcSnapshot.GetLeaf(1, leafIndex).GetRecord(recordType);

            // Calculate opening: distance between leaf tips
            // In IEC, Bank A is on positive X side, Bank B is on negative X side
            // Opening = BankA position - BankB position
            var opening = bankAPos - bankBPos;

            // Only count positive openings (exclude closed/overlapping leaves)
            if (opening > 0)
            {
                totalOpening += opening;
                includedLeafCount++;
            }
        }

        return (totalOpening, includedLeafCount);
    }
}
