using Shouldly;
using TrajectoryLogReader.Fluence;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Tests;

public class FluenceGridSizingTests
{
    [Test]
    public void PixelSize_Creates_Square_Pixels_Covering_Field()
    {
        var data = new FakeFieldDataCollection(new RectField(-43, 17, -30, 55, 1));
        var grid = new FluenceCreator().Create(new FluenceOptions(2.5) { Margin = 0 }, data).Grid;

        grid.XRes.ShouldBe(2.5, 1e-9);
        grid.YRes.ShouldBe(2.5, 1e-9);
        grid.Bounds.X.ShouldBeLessThanOrEqualTo(-43);
        grid.Bounds.Y.ShouldBeLessThanOrEqualTo(-30);
        (grid.Bounds.X + grid.Bounds.Width).ShouldBeGreaterThanOrEqualTo(17);
        (grid.Bounds.Y + grid.Bounds.Height).ShouldBeGreaterThanOrEqualTo(55);
    }

    [Test]
    public void PixelSize_Aligns_Pixel_Centres_For_Different_Fields()
    {
        var small = new FluenceCreator().Create(new FluenceOptions(2),
            new FakeFieldDataCollection(new RectField(-20.3f, 31.7f, -12.1f, 40.9f, 1))).Grid;
        var large = new FluenceCreator().Create(new FluenceOptions(2),
            new FakeFieldDataCollection(new RectField(-75.2f, 60.1f, -80.4f, 42.2f, 1))).Grid;

        // Pixel centres lie on multiples of the pixel size, so both grids share pixel positions
        foreach (var grid in new[] { small, large })
        {
            (grid.GetX(0) / 2).ShouldBe(Math.Round(grid.GetX(0) / 2), 1e-9);
            (grid.GetY(0) / 2).ShouldBe(Math.Round(grid.GetY(0) / 2), 1e-9);
        }
    }

    [Test]
    public void Extent_Ignores_Snapshots_That_Deliver_No_Fluence()
    {
        var data = new FakeFieldDataCollection(
            new RectField(-100, 100, -100, 100, 0),
            new RectField(-100, 100, -100, 100, 1, isBeamHold: true),
            new RectField(-20, 20, -20, 20, 1));

        var grid = new FluenceCreator().Create(new FluenceOptions(10, 10) { Margin = 0 }, data).Grid;

        grid.Bounds.X.ShouldBe(-20, 1e-6);
        grid.Bounds.Width.ShouldBe(40, 1e-6);
    }

    private sealed class FakeFieldDataCollection : List<IFieldData>, IFieldDataCollection
    {
        public FakeFieldDataCollection(params IFieldData[] items) : base(items)
        {
        }
    }

    private sealed class RectField : IFieldData
    {
        private readonly bool _isBeamHold;

        public RectField(float x1, float x2, float y1, float y2, float deltaMu, bool isBeamHold = false)
        {
            X1InMm = x1;
            X2InMm = x2;
            Y1InMm = y1;
            Y2InMm = y2;
            DeltaMu = deltaMu;
            _isBeamHold = isBeamHold;
        }

        public IMLCModel Mlc { get; } = new Millenium120MLC();
        public float X1InMm { get; }
        public float Y1InMm { get; }
        public float X2InMm { get; }
        public float Y2InMm { get; }
        public float GantryInDegrees => 0;
        public float CollimatorInDegrees => 0;
        public float DeltaMu { get; }

        // Leaves retracted behind the jaws
        public float GetLeafPositionInMm(int bank, int leafIndex) => bank == 0 ? X2InMm : X1InMm;

        public bool IsBeamHold() => _isBeamHold;
    }
}
