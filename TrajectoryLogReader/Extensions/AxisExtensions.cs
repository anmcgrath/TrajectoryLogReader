using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Extensions
{
    public static class AxisExtensions
    {
        /// <summary>
        /// Gets the unit of measurement for the specified axis.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <returns>The unit of measurement.</returns>
        public static Unit GetUnit(this Axis axis)
        {
            switch (axis)
            {
                case Axis.CollRtn:
                case Axis.GantryRtn:
                case Axis.CouchRtn:
                case Axis.CouchPitch:
                case Axis.CouchRoll:
                    return Unit.Degrees;

                case Axis.Y1:
                case Axis.Y2:
                case Axis.X1:
                case Axis.X2:
                case Axis.CouchVrt:
                case Axis.CouchLng:
                case Axis.CouchLat:
                case Axis.MLC:
                case Axis.TargetPosition:
                case Axis.TrackingTarget:
                case Axis.TrackingBase:
                    return Unit.Centimeters;

                case Axis.MU:
                    return Unit.MU;

                case Axis.BeamHold:
                case Axis.ControlPoint:
                case Axis.TrackingPhase:
                case Axis.TrackingConformityIndex:
                    return Unit.Dimensionless; // Or None, but Dimensionless implies a value without a unit.

                default:
                    return Unit.None;
            }
        }
    }
}