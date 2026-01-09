using Dicom;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.DICOM;

public class PlanModelReader
{
    private const string JawTypeX = "ASYMX";
    private const string JawTypeY = "ASYMY";
    private const string MLCX = "MLCX";
    private const string MLCY = "MLCY";

    public static PlanModel Read(DicomFile dcm)
    {
        var plan = new PlanModel();

        var beamSequences = dcm.Dataset.GetSequence(DicomTag.BeamSequence);

        foreach (var beamSeq in beamSequences)
        {
            var beam = new BeamModel();
            beam.BeamName = beamSeq.GetSingleValueOrDefault(DicomTag.BeamName, string.Empty);
            beam.BeamNumber = beamSeq.GetSingleValue<int>(DicomTag.BeamNumber);

            foreach (var ds in beamSeq.GetSequence(DicomTag.BeamLimitingDeviceSequence))
            {
                var type = ds.GetSingleValueOrDefault<string>(DicomTag.RTBeamLimitingDeviceType,
                    string.Empty);
                if (type == MLCX)
                {
                    var mlcPos = ds.GetValues<float>(DicomTag.LeafPositionBoundaries);
                    beam.Mlc = new DicomMLC(mlcPos);
                }
            }

            float colAngle = 0; // will be updated if it exists

            foreach (var cp in beamSeq.GetSequence(DicomTag.ControlPointSequence))
            {
                var cpData = new ControlPointData();
                cpData.CumulativeMetersetWeight = cp.GetSingleValue<float>(DicomTag.CumulativeMetersetWeight);
                if (cp.Contains(DicomTag.BeamLimitingDeviceAngle))
                {
                    colAngle = cp.GetSingleValue<float>(DicomTag.BeamLimitingDeviceAngle);
                }

                cpData.CollimatorAngle = colAngle;

                foreach (var beamLimitSeq in cp.GetSequence(DicomTag.BeamLimitingDevicePositionSequence))
                {
                    var type = beamLimitSeq.GetSingleValueOrDefault<string>(DicomTag.RTBeamLimitingDeviceType,
                        string.Empty);
                    var vals = beamLimitSeq.GetValues<float>(DicomTag.LeafJawPositions);
                    switch (type)
                    {
                        case JawTypeX:
                            cpData.X1 = vals[0];
                            cpData.X2 = vals[1];
                            break;
                        case JawTypeY:
                            cpData.Y1 = vals[0];
                            cpData.Y2 = vals[1];
                            break;
                        case MLCX:
                            cpData.MlcData = new float[2, vals.Length / 2];
                            for (int i = 0; i < vals.Length / 2; i++)
                                cpData.MlcData[0, i] = vals[i];
                            var mlcIndex = 0;
                            for (int i = vals.Length / 2; i < vals.Length; i++)
                            {
                                cpData.MlcData[1, mlcIndex] = vals[i];
                                mlcIndex++;
                            }

                            break;
                        default:
                            throw new ApplicationException("Unknown type: " + type);
                    }
                }

                beam.ControlPoints.Add(cpData);
            }


            plan.Beams.Add(beam);
        }

        foreach (var fractionSeq in dcm.Dataset.GetSequence(DicomTag.FractionGroupSequence))
        {
            foreach (var beamSeq in fractionSeq.GetSequence(DicomTag.ReferencedBeamSequence))
            {
                var mu = beamSeq.GetSingleValue<float>(DicomTag.BeamMeterset);
                var refBeamNum = beamSeq.GetSingleValue<int>(DicomTag.ReferencedBeamNumber);
                var beam = plan.Beams.FirstOrDefault(x => x.BeamNumber == refBeamNum);
                if (beam != null)
                    beam.MU = mu;
            }
        }

        return plan;
    }
}