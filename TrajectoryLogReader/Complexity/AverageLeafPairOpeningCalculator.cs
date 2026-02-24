using TrajectoryLogReader.Fluence;

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
    /// <param name="fieldDataCollection">The snapshot collection to analyze.</param>
    /// <param name="options">Calculation options. If null, default options are used.</param>
    /// <returns>The average leaf pair opening in centimeters.</returns>
    public static double Calculate(IFieldDataCollection fieldDataCollection,
        AverageLeafPairOpeningOptions? options = null)
    {
        options ??= new AverageLeafPairOpeningOptions();

        double totalOpening = 0;
        long totalLeafPairCount = 0;

        foreach (var fieldData in fieldDataCollection)
        {
            // Skip if MU is zero and we're not including zero MU snapshots
            if (!options.IncludeZeroMu)
            {
                var deltaMu = fieldData.DeltaMu;
                if (deltaMu <= 0)
                    continue;
            }

            // Skip if beam is on hold and we're not including beam hold snapshots
            if (!options.IncludeBeamHold)
            {
                var beamHold = fieldData.IsBeamHold();
                if (beamHold)
                    continue;
            }

            var (snapshotOpening, leafCount) = CalculateForSnapshot(fieldData);
            totalOpening += snapshotOpening;
            totalLeafPairCount += leafCount;
        }

        return totalLeafPairCount > 0 ? (1d / 10) * totalOpening / totalLeafPairCount : 0;
    }

    /// <summary>
    /// Calculates the total opening and leaf count for a single snapshot.
    /// </summary>
    private static (double totalOpening, int leafCount) CalculateForSnapshot(IFieldData fieldData)
    {
        var mlcModel = fieldData.Mlc;
        var numLeafPairs = mlcModel.GetNumberOfLeafPairs();

        // Get jaw positions in IEC scale (mm)
        var y1Mm = fieldData.Y1InMm;
        var y2Mm = fieldData.Y2InMm;

        // Ensure y1 < y2 (y1 is toward floor/negative, y2 is toward gun/positive)
        if (y1Mm > y2Mm)
            (y1Mm, y2Mm) = (y2Mm, y1Mm);

        double totalOpening = 0;
        int includedLeafCount = 0;

        for (int leafIndex = 0; leafIndex < numLeafPairs; leafIndex++)
        {
            var leafInfo = mlcModel.GetLeafInformation(leafIndex);
            var leafCenterY = leafInfo.YInMm;

            // Check if leaf center is within the Y jaw opening
            if (leafCenterY < y1Mm || leafCenterY > y2Mm)
                continue;

            // Get leaf positions for both banks (in mm, IEC scale)
            var bankBPos = fieldData.GetLeafPositionInMm(0, leafIndex);
            var bankAPos = fieldData.GetLeafPositionInMm(1, leafIndex);

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