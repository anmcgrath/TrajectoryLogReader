using Shouldly;
using TrajectoryLogReader.LogStatistics;

namespace TrajectoryLogReader.Tests;

public class AggregateFunctionTests
{
    private float[] _data;

    [SetUp]
    public void Setup()
    {
        _data =
        [
            -1, 0, 2, 5, 10
        ];
    }

    private float PerformAggregateFunction(Func<AggregateArgs, float> fn)
    {
        float agg = 0;
        for (int i = 0; i < _data.Length; i++)
        {
            agg = fn(new AggregateArgs(agg, _data[i], i));
        }

        return agg;
    }

    [Test]
    public void AverageAggregateFunctionCalculatesAverage()
    {
        var fn = Statistics.GetAggregateFunction(AggregateFunction.Average);
        var result = PerformAggregateFunction(fn);
        result.ShouldBe(_data.Average());
    }

    [Test]
    public void MinAggregateFunctionCalculatesMin()
    {
        var fn = Statistics.GetAggregateFunction(AggregateFunction.Min);
        var result = PerformAggregateFunction(fn);
        result.ShouldBe(_data.Min());
    }

    [Test]
    public void MaxAggregateFunctionCalculatesMax()
    {
        var fn = Statistics.GetAggregateFunction(AggregateFunction.Max);
        var result = PerformAggregateFunction(fn);
        result.ShouldBe(_data.Max());
    }

    [Test]
    public void SumAggregateFunctionCalculatesSum()
    {
        var fn = Statistics.GetAggregateFunction(AggregateFunction.Sum);
        var result = PerformAggregateFunction(fn);
        result.ShouldBe(_data.Sum());
    }

    [Test]
    public void CountAggregateFunctionCalculatesCount()
    {
        var fn = Statistics.GetAggregateFunction(AggregateFunction.Count);
        var result = PerformAggregateFunction(fn);
        result.ShouldBe(_data.Length);
    }
}