namespace TrajectoryLogReader.Log;

/// <summary>
/// Compares two TrajectoryLog instances for equality.
/// Useful for verifying round-trip serialization (read -> write -> read).
/// </summary>
internal static class TrajectoryLogComparer
{
    /// <summary>
    /// Compares two TrajectoryLog instances and returns a result indicating equality.
    /// </summary>
    /// <param name="expected">The expected (reference) trajectory log.</param>
    /// <param name="actual">The actual trajectory log to compare.</param>
    /// <param name="floatTolerance">Tolerance for floating-point comparisons. Default is 1e-6f.</param>
    /// <returns>A comparison result with details of any differences.</returns>
    public static ComparisonResult Compare(TrajectoryLog expected, TrajectoryLog actual, float floatTolerance = 1e-6f)
    {
        var result = new ComparisonResult();

        if (expected == null && actual == null)
            return result;

        if (expected == null)
        {
            result.AddDifference("Root", "Expected is null, actual is not null");
            return result;
        }

        if (actual == null)
        {
            result.AddDifference("Root", "Expected is not null, actual is null");
            return result;
        }

        CompareHeaders(expected.Header, actual.Header, result);
        CompareMetaData(expected.MetaData, actual.MetaData, result);
        CompareSubBeams(expected.SubBeams, actual.SubBeams, result, floatTolerance);
        CompareAxisData(expected, actual, result, floatTolerance);

        return result;
    }

    private static void CompareHeaders(Header expected, Header actual, ComparisonResult result)
    {
        if (expected == null && actual == null)
            return;

        if (expected == null || actual == null)
        {
            result.AddDifference("Header",
                $"Expected: {(expected == null ? "null" : "not null")}, Actual: {(actual == null ? "null" : "not null")}");
            return;
        }

        CompareValue("Header.Version", expected.Version, actual.Version, result);
        CompareValue("Header.SamplingIntervalInMS", expected.SamplingIntervalInMS, actual.SamplingIntervalInMS, result);
        CompareValue("Header.NumAxesSampled", expected.NumAxesSampled, actual.NumAxesSampled, result);
        CompareValue("Header.AxisScale", expected.AxisScale, actual.AxisScale, result);
        CompareValue("Header.NumberOfSubBeams", expected.NumberOfSubBeams, actual.NumberOfSubBeams, result);
        CompareValue("Header.IsTruncated", expected.IsTruncated, actual.IsTruncated, result);
        CompareValue("Header.NumberOfSnapshots", expected.NumberOfSnapshots, actual.NumberOfSnapshots, result);
        CompareValue("Header.MlcModel", expected.MlcModel, actual.MlcModel, result);

        if (expected.AxesSampled?.Length != actual.AxesSampled?.Length)
        {
            result.AddDifference("Header.AxesSampled.Length",
                $"Expected: {expected.AxesSampled?.Length}, Actual: {actual.AxesSampled?.Length}");
        }
        else if (expected.AxesSampled != null && actual.AxesSampled != null)
        {
            for (int i = 0; i < expected.AxesSampled.Length; i++)
            {
                CompareValue($"Header.AxesSampled[{i}]", expected.AxesSampled[i], actual.AxesSampled[i], result);
            }
        }

        if (expected.SamplesPerAxis?.Length != actual.SamplesPerAxis?.Length)
        {
            result.AddDifference("Header.SamplesPerAxis.Length",
                $"Expected: {expected.SamplesPerAxis?.Length}, Actual: {actual.SamplesPerAxis?.Length}");
        }
        else if (expected.SamplesPerAxis != null && actual.SamplesPerAxis != null)
        {
            for (int i = 0; i < expected.SamplesPerAxis.Length; i++)
            {
                CompareValue($"Header.SamplesPerAxis[{i}]", expected.SamplesPerAxis[i], actual.SamplesPerAxis[i],
                    result);
            }
        }
    }

    private static void CompareMetaData(MetaData expected, MetaData actual, ComparisonResult result)
    {
        if (expected == null && actual == null)
            return;

        if (expected == null || actual == null)
        {
            result.AddDifference("MetaData",
                $"Expected: {(expected == null ? "null" : "not null")}, Actual: {(actual == null ? "null" : "not null")}");
            return;
        }

        CompareValue("MetaData.PatientId", expected.PatientId, actual.PatientId, result);
        CompareValue("MetaData.PlanName", expected.PlanName, actual.PlanName, result);
        CompareValue("MetaData.PlanUID", expected.PlanUID, actual.PlanUID, result);
        CompareValue("MetaData.MUPlanned", expected.MUPlanned, actual.MUPlanned, result);
        CompareValue("MetaData.MURemaining", expected.MURemaining, actual.MURemaining, result);
        CompareValue("MetaData.Energy", expected.Energy, actual.Energy, result);
        CompareValue("MetaData.BeamName", expected.BeamName, actual.BeamName, result);
    }

    private static void CompareSubBeams(List<SubBeam> expected, List<SubBeam> actual, ComparisonResult result,
        float tolerance)
    {
        if (expected == null && actual == null)
            return;

        if (expected == null || actual == null)
        {
            result.AddDifference("SubBeams",
                $"Expected: {(expected == null ? "null" : "not null")}, Actual: {(actual == null ? "null" : "not null")}");
            return;
        }

        if (expected.Count != actual.Count)
        {
            result.AddDifference("SubBeams.Count", $"Expected: {expected.Count}, Actual: {actual.Count}");
            return;
        }

        for (int i = 0; i < expected.Count; i++)
        {
            var exp = expected[i];
            var act = actual[i];

            CompareValue($"SubBeams[{i}].ControlPoint", exp.ControlPoint, act.ControlPoint, result);
            CompareFloat($"SubBeams[{i}].MU", exp.MU, act.MU, result, tolerance);
            CompareFloat($"SubBeams[{i}].RadTime", exp.RadTime, act.RadTime, result, tolerance);
            CompareValue($"SubBeams[{i}].SequenceNumber", exp.SequenceNumber, act.SequenceNumber, result);
            CompareValue($"SubBeams[{i}].Name", exp.Name, act.Name, result);
        }
    }

    private static void CompareAxisData(TrajectoryLog expected, TrajectoryLog actual, ComparisonResult result,
        float tolerance)
    {
        if (expected.AxisData == null && actual.AxisData == null)
            return;

        if (expected.AxisData == null || actual.AxisData == null)
        {
            result.AddDifference("AxisData",
                $"Expected: {(expected.AxisData == null ? "null" : "not null")}, Actual: {(actual.AxisData == null ? "null" : "not null")}");
            return;
        }

        if (expected.AxisData.Length != actual.AxisData.Length)
        {
            result.AddDifference("AxisData.Length",
                $"Expected: {expected.AxisData.Length}, Actual: {actual.AxisData.Length}");
            return;
        }

        for (int axisIndex = 0; axisIndex < expected.AxisData.Length; axisIndex++)
        {
            var expAxis = expected.AxisData[axisIndex];
            var actAxis = actual.AxisData[axisIndex];

            if (expAxis.NumSnapshots != actAxis.NumSnapshots)
            {
                result.AddDifference($"AxisData[{axisIndex}].NumSnapshots",
                    $"Expected: {expAxis.NumSnapshots}, Actual: {actAxis.NumSnapshots}");
                continue;
            }

            if (expAxis.SamplesPerSnapshot != actAxis.SamplesPerSnapshot)
            {
                result.AddDifference($"AxisData[{axisIndex}].SamplesPerSnapshot",
                    $"Expected: {expAxis.SamplesPerSnapshot}, Actual: {actAxis.SamplesPerSnapshot}");
                continue;
            }

            var expData = expAxis.Data;
            var actData = actAxis.Data;

            if (expData.Length != actData.Length)
            {
                result.AddDifference($"AxisData[{axisIndex}].Data.Length",
                    $"Expected: {expData.Length}, Actual: {actData.Length}");
                continue;
            }

            int mismatchCount = 0;
            int firstMismatchIndex = -1;
            float firstExpected = 0, firstActual = 0;

            for (int i = 0; i < expData.Length; i++)
            {
                if (Math.Abs(expData[i] - actData[i]) > tolerance)
                {
                    mismatchCount++;
                    if (firstMismatchIndex < 0)
                    {
                        firstMismatchIndex = i;
                        firstExpected = expData[i];
                        firstActual = actData[i];
                    }
                }
            }

            if (mismatchCount > 0)
            {
                var axisName = axisIndex < expected.Header.AxesSampled.Length
                    ? expected.Header.AxesSampled[axisIndex].ToString()
                    : axisIndex.ToString();

                result.AddDifference($"AxisData[{axisIndex}] ({axisName})",
                    $"{mismatchCount} value mismatches. First at index {firstMismatchIndex}: Expected {firstExpected}, Actual {firstActual}");
            }
        }
    }

    private static void CompareValue<T>(string path, T expected, T actual, ComparisonResult result)
    {
        if (!Equals(expected, actual))
        {
            result.AddDifference(path, $"Expected: {expected}, Actual: {actual}");
        }
    }

    private static void CompareFloat(string path, float expected, float actual, ComparisonResult result,
        float tolerance)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            result.AddDifference(path, $"Expected: {expected}, Actual: {actual} (diff: {Math.Abs(expected - actual)})");
        }
    }
}

/// <summary>
/// Result of comparing two TrajectoryLog instances.
/// </summary>
public class ComparisonResult
{
    private readonly List<Difference> _differences = new();

    /// <summary>
    /// True if the two logs are equal (no differences found).
    /// </summary>
    public bool AreEqual => _differences.Count == 0;

    /// <summary>
    /// List of differences found between the two logs.
    /// </summary>
    public IReadOnlyList<Difference> Differences => _differences;

    /// <summary>
    /// Number of differences found.
    /// </summary>
    public int DifferenceCount => _differences.Count;

    internal void AddDifference(string path, string description)
    {
        _differences.Add(new Difference(path, description));
    }

    /// <summary>
    /// Returns a summary of all differences.
    /// </summary>
    public override string ToString()
    {
        if (AreEqual)
            return "Logs are equal.";

        return $"Found {_differences.Count} difference(s):\n" +
               string.Join("\n", _differences.Select(d => $"  - {d.Path}: {d.Description}"));
    }
}

/// <summary>
/// Represents a single difference between two TrajectoryLog instances.
/// </summary>
public class Difference
{
    /// <summary>
    /// The path to the differing property (e.g., "Header.Version" or "AxisData[0].Data[5]").
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Description of the difference.
    /// </summary>
    public string Description { get; }

    internal Difference(string path, string description)
    {
        Path = path;
        Description = description;
    }

    public override string ToString() => $"{Path}: {Description}";
}