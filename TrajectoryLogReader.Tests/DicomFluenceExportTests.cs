using FellowOakDicom;
using FellowOakDicom.Imaging;
using Shouldly;
using TrajectoryLogReader.DICOM;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Tests;

public class DicomFluenceExportTests
{
    [Test]
    public void SaveToDicom_Writes_Geometry_And_Pixels_From_Top_Left()
    {
        // Asymmetric bounds, 3 cols x 2 rows, 2 mm x 5 mm pixels. Value = 10 * row + col.
        var grid = new GridF(new Rect(-10, 4, 6, 10), 3, 2);
        for (int row = 0; row < grid.Rows; row++)
        for (int col = 0; col < grid.Cols; col++)
            grid.SetData(col, row, 10 * row + col);

        var file = Path.GetTempFileName();
        try
        {
            grid.SaveToDicom(file, "Test", "123");
            var ds = DicomFile.Open(file).Dataset;

            ds.GetValues<double>(DicomTag.ImagePlanePixelSpacing).ShouldBe(new[] { 5.0, 2.0 });
            // Centre of the top-left pixel
            ds.GetValues<double>(DicomTag.RTImagePosition).ShouldBe(new[] { -9.0, 11.5 });
            ds.GetSingleValue<double>(DicomTag.RTImageSID).ShouldBe(1000);
            ds.GetSingleValue<double>(DicomTag.RadiationMachineSAD).ShouldBe(1000);

            var slope = ds.GetSingleValue<double>(DicomTag.RescaleSlope);
            var intercept = ds.GetSingleValue<double>(DicomTag.RescaleIntercept);
            var bytes = DicomPixelData.Create(ds).GetFrame(0).Data;
            var values = Enumerable.Range(0, 6)
                .Select(i => BitConverter.ToUInt16(bytes, 2 * i) * slope + intercept)
                .ToArray();

            // First DICOM row is the top (+Y) grid row
            values.ShouldBe(new[] { 10.0, 11, 12, 0, 1, 2 }, 1e-3);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
