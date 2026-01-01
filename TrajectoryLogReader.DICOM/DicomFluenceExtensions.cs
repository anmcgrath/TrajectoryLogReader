using System.Globalization;
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.DICOM;

using System.Linq;

public static class DicomFluenceExtensions
{
    public static void SaveToDicom(this FieldFluence fluence, string fileName, string patientName, string patientId)
    {
        var fluenceGrid = fluence.Grid.Data;
        var spacingX = fluence.Grid.XRes * 10;
        var spacingY = fluence.Grid.YRes * 10;
        int rows = fluence.Grid.SizeY;
        int cols = fluence.Grid.SizeX;

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
            for (int j = 0; j < cols; j++)
            {
                // SV = (Value - Intercept) / Slope
                pixelData[i * cols + j] = (ushort)((fluenceGrid[i, j] - rescaleIntercept) / rescaleSlope);
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
            $"{posX.ToString("G12", CultureInfo.InvariantCulture)}\\{posY.ToString("G12", CultureInfo.InvariantCulture)}");

        // --- IMAGE STRUCTURE ---
        dataset.Add(DicomTag.SamplesPerPixel, (ushort)1);
        dataset.Add(DicomTag.PhotometricInterpretation, "MONOCHROME2");
        dataset.Add(DicomTag.Rows, (ushort)rows);
        dataset.Add(DicomTag.Columns, (ushort)cols);
        dataset.Add(DicomTag.BitsAllocated, (ushort)16);
        dataset.Add(DicomTag.BitsStored, (ushort)16);
        dataset.Add(DicomTag.HighBit, (ushort)15);
        dataset.Add(DicomTag.PixelRepresentation, (ushort)0); // Unsigned


        dataset.Add(DicomTag.RescaleSlope, rescaleSlope.ToString("G12", CultureInfo.InvariantCulture));
        dataset.Add(DicomTag.RescaleIntercept, rescaleIntercept.ToString("G12", CultureInfo.InvariantCulture));
        dataset.Add(DicomTag.RescaleType, "RELATIVE");

        // Pixel Data (using the helper)
        var dicomPixelData = DicomPixelData.Create(dataset, true);
        byte[] rawBytes = new byte[pixelData.Length * 2];
        Buffer.BlockCopy(pixelData, 0, rawBytes, 0, rawBytes.Length);
        dicomPixelData.AddFrame(new MemoryByteBuffer(rawBytes));
        
        var file = new DicomFile(dataset);
        file.Save(fileName);
    }
}