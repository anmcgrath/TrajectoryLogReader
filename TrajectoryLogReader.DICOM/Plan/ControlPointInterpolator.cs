namespace TrajectoryLogReader.DICOM.Plan;

/// <summary>
/// Interpolates between two control points.
/// </summary>
public class ControlPointInterpolator
{
    /// <summary>
    /// Interpolates control point data at a fractional index.
    /// </summary>
    /// <param name="controlPoint1">The first <see cref="ControlPointData"/></param>
    /// <param name="controlPoint2"></param>
    /// <param name="fractionalControlPoint">The fractional control point index.</param>
    /// <returns>Interpolated control point data.</returns>
    public static ControlPointData Interpolate(
        ControlPointData controlPoint1,
        ControlPointData controlPoint2,
        double fractionalControlPoint)
    {
        if (controlPoint1.ControlPointIndex == controlPoint2.ControlPointIndex)
        {
            return controlPoint1;
        }

        float t = (float)(fractionalControlPoint - controlPoint1.ControlPointIndex) /
                  (float)(controlPoint2.ControlPointIndex - controlPoint1.ControlPointIndex);

        var result = new ControlPointData
        {
            ControlPointIndex = (int)Math.Round(fractionalControlPoint),
            CumulativeMetersetWeight = Lerp(controlPoint1.CumulativeMetersetWeight,
                controlPoint2.CumulativeMetersetWeight, t),
            GantryAngle = Lerp(controlPoint1.GantryAngle, controlPoint2.GantryAngle, t),
            CollimatorAngle = Lerp(controlPoint1.CollimatorAngle, controlPoint2.CollimatorAngle, t),
            X1 = Lerp(controlPoint1.X1, controlPoint2.X1, t),
            X2 = Lerp(controlPoint1.X2, controlPoint2.X2, t),
            Y1 = Lerp(controlPoint1.Y1, controlPoint2.Y1, t),
            Y2 = Lerp(controlPoint1.Y2, controlPoint2.Y2, t)
        };

        int banks = controlPoint1.MlcData.GetLength(0);
        int leaves = controlPoint1.MlcData.GetLength(1);

        // Ensure dimensions match
        if (controlPoint2.MlcData.GetLength(0) == banks && controlPoint2.MlcData.GetLength(1) == leaves)
        {
            result.MlcData = new float[banks, leaves];
            for (int i = 0; i < banks; i++)
            {
                for (int j = 0; j < leaves; j++)
                {
                    result.MlcData[i, j] = Lerp(controlPoint1.MlcData[i, j], controlPoint2.MlcData[i, j], t);
                }
            }
        }

        return result;
    }

    private static float Lerp(float v0, float v1, float t)
    {
        return v0 + t * (v1 - v0);
    }
}