using System.Globalization;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using TrajectoryLogReader.DICOM.FluenceAdapters;
using TrajectoryLogReader.DICOM.Plan;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.DICOM;

using System.Linq;

public static class DicomFluenceExtensions
{
    /// <summary>
    /// Saves a fluence map as a DICOM RT Image for interoperability with clinical tooling.
    /// The resulting dataset uses derived RTIMAGE semantics and encodes fluence as a
    /// rescaled 16-bit grayscale image.
    /// </summary>
    /// <param name="fluence">The field fluence.</param>
    /// <param name="fileName">The destination DICOM file path.</param>
    /// <param name="patientName">Patient name to embed in the DICOM header.</param>
    /// <param name="patientId">Patient identifier to embed in the DICOM header.</param>
    public static void SaveToDicom(this FieldFluence fluence, string fileName, string patientName, string patientId)
        => fluence.Grid.SaveToDicom(fileName, patientName, patientId);

    /// <summary>
    /// Saves a numeric grid as a DICOM RT Image. Pixel values are linearly rescaled into
    /// unsigned 16-bit storage, with the rescale slope/intercept recorded so that the
    /// original floating-point values can be reconstructed downstream.
    /// </summary>
    /// <param name="grid">The fluence-like grid to save (units are user-defined).</param>
    /// <param name="fileName">The destination DICOM file path.</param>
    /// <param name="patientName">Patient name to embed in the DICOM header.</param>
    /// <param name="patientId">Patient identifier to embed in the DICOM header.</param>
    public static void SaveToDicom(this IGrid<float> grid, string fileName, string patientName, string patientId)
    {
        var fluenceGrid = grid.Flatten();
        var spacingX = grid.XRes;
        var spacingY = grid.YRes;
        int rows = grid.Rows;
        int cols = grid.Cols;

        // 1. Calculate Scaling (Map float grid to 16-bit unsigned integer)
        float maxVal = fluenceGrid.Cast<float>().Max();
        float minVal = fluenceGrid.Cast<float>().Min();

        // We use a small epsilon to avoid division by zero if grid is empty
        double range = Math.Max(maxVal - minVal, 1e-10);
        double rescaleSlope = range / 65535.0;
        double rescaleIntercept = minVal;

        ushort[] pixelData = new ushort[rows * cols];
        for (int i = 0; i < rows; i++)
        {
            int rowOffset = i * cols;
            for (int j = 0; j < cols; j++)
            {
                // SV = (Value - Intercept) / Slope
                pixelData[rowOffset + j] = (ushort)((fluenceGrid[rowOffset + j] - rescaleIntercept) / rescaleSlope);
            }
        }

        // 2. Initialize Dataset
        var dataset = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian);

        // --- IDENTITY & HIERARCHY ---
        dataset.Add(DicomTag.SOPClassUID, DicomUID.RTImageStorage);
        dataset.Add(DicomTag.SOPInstanceUID, DicomUID.Generate());
        dataset.Add(DicomTag.StudyInstanceUID, DicomUID.Generate());
        dataset.Add(DicomTag.SeriesInstanceUID, DicomUID.Generate());
        dataset.Add(DicomTag.Modality, "RTIMAGE");
        dataset.Add(DicomTag.PatientName, patientName);
        dataset.Add(DicomTag.PatientID, patientId);

        // --- RT IMAGE SPECIFIC TAGS ---
        dataset.Add(DicomTag.ImageType, "DERIVED", "SECONDARY", "FLUENCE");
        dataset.Add(DicomTag.RTImageLabel, "FLUENCE_MAP");

        // Image Plane Pixel Spacing (X and Y distance in mm)
        dataset.Add(DicomTag.ImagePlanePixelSpacing,
            $"{spacingY.ToString("G12", CultureInfo.InvariantCulture)}\\{spacingX.ToString("G12", CultureInfo.InvariantCulture)}");

        // RT Image Position: X and Y of the top-left pixel relative to Beam Central Axis
        // Centering the grid:
        double posX = -(cols * spacingX / 2.0);
        double posY = -(rows * spacingY / 2.0);
        dataset.Add(DicomTag.RTImagePosition,
            $"{posX.ToString("G10", CultureInfo.InvariantCulture)}\\{posY.ToString("G10", CultureInfo.InvariantCulture)}");

        // --- IMAGE STRUCTURE ---
        dataset.Add(DicomTag.SamplesPerPixel, (ushort)1);
        dataset.Add(DicomTag.PhotometricInterpretation, "MONOCHROME2");
        dataset.Add(DicomTag.Rows, (ushort)rows);
        dataset.Add(DicomTag.Columns, (ushort)cols);
        dataset.Add(DicomTag.BitsAllocated, (ushort)16);
        dataset.Add(DicomTag.BitsStored, (ushort)16);
        dataset.Add(DicomTag.HighBit, (ushort)15);
        dataset.Add(DicomTag.PixelRepresentation, (ushort)0); // Unsigned


        dataset.Add(DicomTag.RescaleSlope, rescaleSlope.ToString("G10", CultureInfo.InvariantCulture));
        dataset.Add(DicomTag.RescaleIntercept, rescaleIntercept.ToString("G10", CultureInfo.InvariantCulture));
        dataset.Add(DicomTag.RescaleType, "RELATIVE");

        // Pixel Data (using the helper)
        var dicomPixelData = DicomPixelData.Create(dataset, true);
        byte[] rawBytes = new byte[pixelData.Length * 2];
        Buffer.BlockCopy(pixelData, 0, rawBytes, 0, rawBytes.Length);
        dicomPixelData.AddFrame(new MemoryByteBuffer(rawBytes));

        var file = new DicomFile(dataset);
        file.Save(fileName);
    }

    /// <summary>
    /// Creates a fluence map from a DICOM RT Plan beam. Control points are optionally
    /// interpolated so the fluence accumulation better reflects continuous delivery.
    /// </summary>
    /// <param name="beam">The beam model parsed from an RT Plan.</param>
    /// <param name="options">Fluence grid and accumulation options.</param>
    /// <param name="cpDelta">
    /// Set to control the fractional control points that are included in the fluence.
    /// When set to 1, the fluence uses each control point as-is. When set below 1,
    /// intermediate control points are interpolated (for example 0.5 samples twice per
    /// control-point interval).
    /// </param>
    /// <returns>A fluence map derived from the beam definition.</returns>
    public static FieldFluence CreateFluence(this BeamModel beam, FluenceOptions options, double cpDelta = 1)
    {
        return new FluenceCreator().Create(options, new BeamCollectionAdapter(beam, cpDelta));
    }
}
