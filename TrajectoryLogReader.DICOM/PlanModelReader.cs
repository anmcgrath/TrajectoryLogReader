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

        plan.PatientName = dcm.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
        plan.PatientID = dcm.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
        plan.PlanName = dcm.Dataset.GetSingleValueOrDefault(DicomTag.RTPlanLabel, string.Empty);
        plan.SOPInstanceUID = dcm.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
        plan.SeriesInstanceUID = dcm.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
        plan.StudyInstanceUID = dcm.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
        plan.PlanIntent = dcm.Dataset.GetSingleValueOrDefault(DicomTag.PlanIntent, string.Empty);
        plan.PlanDate = dcm.Dataset.GetSingleValueOrDefault(DicomTag.RTPlanDate, string.Empty);
        plan.PlanTime = dcm.Dataset.GetSingleValueOrDefault(DicomTag.RTPlanTime, string.Empty);
        plan.PlanDescription = dcm.Dataset.GetSingleValueOrDefault(DicomTag.RTPlanDescription, string.Empty);
        plan.TreatmentSite = dcm.Dataset.GetSingleValueOrDefault(DicomTag.TreatmentSite, string.Empty);
        var beamSequences = dcm.Dataset.GetSequence(DicomTag.BeamSequence);

        foreach (var beamSeq in beamSequences)
        {
            var beam = new BeamModel();
            beam.BeamName = beamSeq.GetSingleValueOrDefault(DicomTag.BeamName, string.Empty);
            beam.BeamNumber = beamSeq.GetSingleValue<int>(DicomTag.BeamNumber);

            var dosimeterUnit = beamSeq.GetSingleValueOrDefault(DicomTag.PrimaryDosimeterUnit, string.Empty);
            if (Enum.TryParse<PrimaryDosimeterUnit>(dosimeterUnit, true, out var pdu))
            {
                beam.PrimaryDosimeterUnit = pdu;
            }
            else
            {
                beam.PrimaryDosimeterUnit = PrimaryDosimeterUnit.Unknown;
            }

            beam.NumberOfControlPoints = beamSeq.GetSingleValue<int>(DicomTag.NumberOfControlPoints);

            var radType = beamSeq.GetSingleValueOrDefault(DicomTag.RadiationType, string.Empty);
            if (Enum.TryParse<RadiationType>(radType, true, out var rt))
            {
                beam.RadiationType = rt;
            }
            else
            {
                beam.RadiationType = RadiationType.Unknown;
            }

            var beamType = beamSeq.GetSingleValueOrDefault(DicomTag.BeamType, string.Empty);
            if (Enum.TryParse<BeamType>(beamType, true, out var bt))
            {
                beam.BeamType = bt;
            }
            else
            {
                beam.BeamType = BeamType.Unknown;
            }

            beam.BeamDescription = beamSeq.GetSingleValueOrDefault(DicomTag.BeamDescription, string.Empty);
            beam.TreatmentMachineName = beamSeq.GetSingleValueOrDefault(DicomTag.TreatmentMachineName, string.Empty);

            var fluenceModeSeq = beamSeq.GetSequence(DicomTag.PrimaryFluenceModeSequence);
            if (fluenceModeSeq != null && fluenceModeSeq.Items.Count > 0)
            {
                var modeId = fluenceModeSeq.First().GetSingleValueOrDefault(DicomTag.FluenceModeID, string.Empty);
                if (string.IsNullOrEmpty(modeId))
                {
                    modeId = fluenceModeSeq.First().GetSingleValueOrDefault(DicomTag.FluenceMode, string.Empty);
                }

                if (modeId == "NON_STANDARD")
                {
                    beam.PrimaryFluenceMode = FluenceMode.NonStandard;
                }
                else if (modeId.Contains("FFF")) // Common convention, though explicit ID might vary
                {
                    beam.PrimaryFluenceMode = FluenceMode.FFF;
                }
                else
                {
                    beam.PrimaryFluenceMode = FluenceMode.Standard;
                }
            }
            else
            {
                beam.PrimaryFluenceMode = FluenceMode.Standard;
            }

            if (beamSeq.Contains(DicomTag.ApplicatorSequence))
            {
                foreach (var item in beamSeq.GetSequence(DicomTag.ApplicatorSequence))
                {
                    var app = new ApplicatorModel();
                    app.ApplicatorID = item.GetSingleValueOrDefault(DicomTag.ApplicatorID, string.Empty);
                    app.ApplicatorType = item.GetSingleValueOrDefault(DicomTag.ApplicatorType, string.Empty);
                    app.ApplicatorDescription =
                        item.GetSingleValueOrDefault(DicomTag.ApplicatorDescription, string.Empty);
                    beam.Applicators.Add(app);
                }
            }

            if (beamSeq.Contains(DicomTag.BlockSequence))
            {
                foreach (var item in beamSeq.GetSequence(DicomTag.BlockSequence))
                {
                    var block = new BlockModel();
                    block.BlockNumber = item.GetSingleValue<int>(DicomTag.BlockNumber);
                    block.BlockName = item.GetSingleValueOrDefault(DicomTag.BlockName, string.Empty);
                    block.BlockType = item.GetSingleValueOrDefault(DicomTag.BlockType, string.Empty);
                    block.BlockTrayID = item.GetSingleValueOrDefault(DicomTag.BlockTrayID, string.Empty);
                    beam.Blocks.Add(block);
                }
            }

            if (beamSeq.Contains(DicomTag.WedgeSequence))
            {
                foreach (var item in beamSeq.GetSequence(DicomTag.WedgeSequence))
                {
                    var wedge = new WedgeModel();
                    wedge.WedgeNumber = item.GetSingleValue<int>(DicomTag.WedgeNumber);
                    wedge.WedgeID = item.GetSingleValueOrDefault(DicomTag.WedgeID, string.Empty);
                    wedge.WedgeType = item.GetSingleValueOrDefault(DicomTag.WedgeType, string.Empty);
                    wedge.WedgeAngle = item.GetSingleValueOrDefault<float>(DicomTag.WedgeAngle, 0);
                    beam.Wedges.Add(wedge);
                }
            }

            if (beam.PrimaryDosimeterUnit != PrimaryDosimeterUnit.MU)
            {
                throw new ApplicationException(
                    $"Primary dosimeter unit must be MU, found: {beam.PrimaryDosimeterUnit}");
            }

            if (beamSeq.Contains(DicomTag.BeamLimitingDeviceSequence))
            {
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
            }

            float colAngle = 0; // will be updated if it exists
            float gantryAngle = 0;

            if (beamSeq.Contains(DicomTag.ControlPointSequence))
            {
                var cpSeq = beamSeq.GetSequence(DicomTag.ControlPointSequence);
                if (cpSeq.Items.Count > 0)
                {
                    var firstCp = cpSeq.First();
                    beam.Energy = firstCp.GetSingleValueOrDefault<float>(DicomTag.NominalBeamEnergy, 0f);

                    foreach (var cp in cpSeq)
                    {
                        var cpData = new ControlPointData();
                        cpData.CumulativeMetersetWeight = cp.GetSingleValue<float>(DicomTag.CumulativeMetersetWeight);
                        if (cp.Contains(DicomTag.BeamLimitingDeviceAngle))
                        {
                            colAngle = cp.GetSingleValue<float>(DicomTag.BeamLimitingDeviceAngle);
                        }

                        if (cp.Contains(DicomTag.GantryAngle))
                        {
                            gantryAngle = cp.GetSingleValue<float>(DicomTag.GantryAngle);
                        }

                        cpData.CollimatorAngle = colAngle;
                        cpData.GantryAngle = gantryAngle;

                        if (cp.Contains(DicomTag.BeamLimitingDevicePositionSequence))
                        {
                            foreach (var beamLimitSeq in cp.GetSequence(DicomTag.BeamLimitingDevicePositionSequence))
                            {
                                var type = beamLimitSeq.GetSingleValueOrDefault<string>(
                                    DicomTag.RTBeamLimitingDeviceType,
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
                        }

                        beam.ControlPoints.Add(cpData);
                    }
                }
            }


            plan.Beams.Add(beam);
        }

        foreach (var fractionSeq in dcm.Dataset.GetSequence(DicomTag.FractionGroupSequence))
        {
            var fraction = new FractionModel();
            fraction.FractionGroupNumber = fractionSeq.GetSingleValue<int>(DicomTag.FractionGroupNumber);
            fraction.NumberOfFractionsPlanned = fractionSeq.GetSingleValue<int>(DicomTag.NumberOfFractionsPlanned);
            fraction.NumberOfBeams = fractionSeq.GetSingleValue<int>(DicomTag.NumberOfBeams);
            plan.Fractions.Add(fraction);

            if (fractionSeq.Contains(DicomTag.ReferencedBeamSequence))
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
        }

        if (dcm.Dataset.Contains(DicomTag.DoseReferenceSequence))
        {
            foreach (var item in dcm.Dataset.GetSequence(DicomTag.DoseReferenceSequence))
            {
                var prescription = new PrescriptionModel();
                prescription.DoseReferenceNumber = item.GetSingleValue<int>(DicomTag.DoseReferenceNumber);
                prescription.DoseReferenceStructureType =
                    item.GetSingleValueOrDefault(DicomTag.DoseReferenceStructureType, string.Empty);
                prescription.DoseReferenceDescription =
                    item.GetSingleValueOrDefault(DicomTag.DoseReferenceDescription, string.Empty);
                prescription.DoseReferenceType =
                    item.GetSingleValueOrDefault(DicomTag.DoseReferenceType, string.Empty);
                prescription.TargetPrescriptionDose =
                    item.GetSingleValueOrDefault<float>(DicomTag.TargetPrescriptionDose, 0f);
                plan.Prescriptions.Add(prescription);
            }
        }

        return plan;
    }
}