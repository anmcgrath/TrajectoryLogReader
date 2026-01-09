using TrajectoryLogReader.Log;

namespace TrajectoryLogReader.Util;

public interface IScaleConverter
{
    float ToIec(Axis axis, float value);
    float FromIec(Axis axis, float value);
    float MlcPositionToIec(int bank, float value);
    float MlcPositionFromIec(int bank, float value);
}