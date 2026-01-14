namespace TrajectoryLogReader.LogStatistics;

public class AggregateArgs
{
    /// <summary>
    /// The current aggregate value (e.g current sum)
    /// </summary>
    public float CurrentAggregateValue { get; }

    /// <summary>
    /// The value of the data to aggregate
    /// </summary>
    public float DataValue { get; }

    /// <summary>
    /// The number of values included in the aggregate
    /// </summary>
    public int AggregateCounts { get; }

    public AggregateArgs(float currentAggregateValue, float dataValue, int aggregateCounts)
    {
        CurrentAggregateValue = currentAggregateValue;
        DataValue = dataValue;
        AggregateCounts = aggregateCounts;
    }
}