using Dicom;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.DICOM.Plan;

public class PlanModelReader
{
    private const string JawTypeX = "ASYMX";
    private const string JawTypeY = "ASYMY";
    private const string MLCX = "MLCX";
    private const string MLCY = "MLCY";

    public static PlanModel Read(string dicomFile)
    {
        if (!File.Exists(dicomFile))
            return new PlanModel();
        if (!DicomFile.HasValidHeader(dicomFile))
            return new PlanModel();
        return Read(DicomFile.Open(dicomFile));
    }

    public static PlanModel Read(Stream stream)
    {
        return Read(DicomFile.Open(stream));
    }

    public static async Task<PlanModel> ReadAsync(string dicomFile)
    {
        if (!File.Exists(dicomFile))
            return new PlanModel();
        if (!DicomFile.HasValidHeader(dicomFile))
            return new PlanModel();
        return Read(await DicomFile.OpenAsync(dicomFile));
    }

    public static async Task<PlanModel> ReadAsync(Stream stream)
    {
        return Read(await DicomFile.OpenAsync(stream));
    }

    /// <summary>
    /// Reads a plan model from a DICOM file.
    /// </summary>
    /// <param name="dcm">The DicomFile object.</param>
    /// <returns>A PlanModel.</returns>
    internal static PlanModel Read(DicomFile dcm)
    {
        var plan = ReadPlanGeneralInfo(dcm.Dataset);
        plan.Beams = ReadBeams(dcm.Dataset);
        ReadFractions(dcm.Dataset, plan);
        ReadPrescriptions(dcm.Dataset, plan);
        return plan;
    }

    private static PlanModel ReadPlanGeneralInfo(DicomDataset dataset)
    {
        var plan = new PlanModel();
        plan.PatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
        plan.PatientID = dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
        plan.PlanName = dataset.GetSingleValueOrDefault(DicomTag.RTPlanLabel, string.Empty);
        plan.SOPInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
        plan.SeriesInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
        plan.StudyInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
        plan.PlanIntent = dataset.GetSingleValueOrDefault(DicomTag.PlanIntent, string.Empty);
        if (dataset.Contains(DicomTag.RTPlanDate))
        {
            plan.PlanTimestamp = dataset.GetDateTime(DicomTag.RTPlanDate, DicomTag.RTPlanTime);
        }

        plan.PlanDescription = dataset.GetSingleValueOrDefault(DicomTag.RTPlanDescription, string.Empty);
        plan.TreatmentSite = dataset.GetSingleValueOrDefault(DicomTag.TreatmentSite, string.Empty);
        return plan;
    }

    private static List<BeamModel> ReadBeams(DicomDataset dataset)
    {
        var beams = new List<BeamModel>();
        var beamSequences = dataset.GetSequence(DicomTag.BeamSequence);
        foreach (var beamSeq in beamSequences)
        {
            beams.Add(ReadBeam(beamSeq));
        }

        return beams;
    }

    private static BeamModel ReadBeam(DicomDataset beamSeq)
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

        ReadFluenceMode(beamSeq, beam);
        ReadApplicators(beamSeq, beam);
        ReadBlocks(beamSeq, beam);
        ReadWedges(beamSeq, beam);

        if (beam.PrimaryDosimeterUnit != PrimaryDosimeterUnit.MU)
        {
            throw new ApplicationException($"Primary dosimeter unit must be MU, found: {beam.PrimaryDosimeterUnit}");
        }

        beam.Mlc = ReadMlcModel(beamSeq);
        ReadControlPoints(beamSeq, beam);

        return beam;
    }

    private static void ReadFluenceMode(DicomDataset beamSeq, BeamModel beam)
    {
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
            else if (modeId.Contains("FFF"))
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
    }

    private static void ReadApplicators(DicomDataset beamSeq, BeamModel beam)
    {
        if (beamSeq.Contains(DicomTag.ApplicatorSequence))
        {
            foreach (var item in beamSeq.GetSequence(DicomTag.ApplicatorSequence))
            {
                var app = new ApplicatorModel();
                app.ApplicatorID = item.GetSingleValueOrDefault(DicomTag.ApplicatorID, string.Empty);
                app.ApplicatorType = item.GetSingleValueOrDefault(DicomTag.ApplicatorType, string.Empty);
                app.ApplicatorDescription = item.GetSingleValueOrDefault(DicomTag.ApplicatorDescription, string.Empty);
                beam.Applicators.Add(app);
            }
        }
    }

    private static void ReadBlocks(DicomDataset beamSeq, BeamModel beam)
    {
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
    }

    private static void ReadWedges(DicomDataset beamSeq, BeamModel beam)
    {
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
    }

    private static IMLCModel ReadMlcModel(DicomDataset beamSeq)
    {
        if (beamSeq.Contains(DicomTag.BeamLimitingDeviceSequence))
        {
            foreach (var ds in beamSeq.GetSequence(DicomTag.BeamLimitingDeviceSequence))
            {
                var type = ds.GetSingleValueOrDefault<string>(DicomTag.RTBeamLimitingDeviceType, string.Empty);
                if (type == MLCX)
                {
                    var mlcPos = ds.GetValues<float>(DicomTag.LeafPositionBoundaries);
                    return new DicomMLC(mlcPos);
                }
            }
        }

        return null;
    }

    private static void ReadControlPoints(DicomDataset beamSeq, BeamModel beam)
    {
        float colAngle = 0;
        float gantryAngle = 0;

        if (beamSeq.Contains(DicomTag.ControlPointSequence))
        {
            var cpSeq = beamSeq.GetSequence(DicomTag.ControlPointSequence);
            if (cpSeq.Items.Count > 0)
            {
                var firstCp = cpSeq.First();
                beam.Energy = firstCp.GetSingleValueOrDefault<float>(DicomTag.NominalBeamEnergy, 0f);

                int cpIndex = 0;
                foreach (var cp in cpSeq)
                {
                    var cpData = new ControlPointData();
                    cpData.ControlPointIndex = cpIndex++;
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

                    ReadBeamLimitingDevices(cp, cpData);
                    beam.ControlPoints.Add(cpData);
                }
            }
        }
    }

    private static void ReadBeamLimitingDevices(DicomDataset cp, ControlPointData cpData)
    {
        if (cp.Contains(DicomTag.BeamLimitingDevicePositionSequence))
        {
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
        }
    }

    private static void ReadFractions(DicomDataset dataset, PlanModel plan)
    {
        if (dataset.Contains(DicomTag.FractionGroupSequence))
        {
            foreach (var fractionSeq in dataset.GetSequence(DicomTag.FractionGroupSequence))
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
        }
    }

    private static void ReadPrescriptions(DicomDataset dataset, PlanModel plan)
    {
        if (dataset.Contains(DicomTag.DoseReferenceSequence))
        {
            foreach (var item in dataset.GetSequence(DicomTag.DoseReferenceSequence))
            {
                var prescription = new PrescriptionModel();
                prescription.DoseReferenceNumber = item.GetSingleValue<int>(DicomTag.DoseReferenceNumber);
                prescription.DoseReferenceStructureType =
                    item.GetSingleValueOrDefault(DicomTag.DoseReferenceStructureType, string.Empty);
                prescription.DoseReferenceDescription =
                    item.GetSingleValueOrDefault(DicomTag.DoseReferenceDescription, string.Empty);
                prescription.DoseReferenceType = item.GetSingleValueOrDefault(DicomTag.DoseReferenceType, string.Empty);
                prescription.TargetPrescriptionDose =
                    item.GetSingleValueOrDefault<float>(DicomTag.TargetPrescriptionDose, 0f);
                plan.Prescriptions.Add(prescription);
            }
        }
    }
}