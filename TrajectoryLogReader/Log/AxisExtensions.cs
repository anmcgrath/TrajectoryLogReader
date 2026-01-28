namespace TrajectoryLogReader.Log;

public static class AxisExtensions
{
    public static bool IsRotational(this Axis axis)
    {
        return axis == Axis.GantryRtn ||
               axis == Axis.CollRtn ||
               axis == Axis.CouchRtn ||
               axis == Axis.CouchPitch ||
               axis == Axis.CouchRoll;
    }
}
