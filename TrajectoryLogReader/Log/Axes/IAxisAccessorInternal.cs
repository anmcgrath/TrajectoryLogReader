namespace TrajectoryLogReader.Log.Axes;

internal interface IAxisAccessorInternal : IAxisAccessor
{
    int SampleRateInMs { get; }

    AxisScale GetSourceScale();

    AxisScale GetEffectiveScale();
}
