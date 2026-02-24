using Shouldly;
using TrajectoryLogReader.Complexity;
using TrajectoryLogReader.Fluence;
using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Tests;

[TestFixture]
public class AverageLeafPairOpeningCalculatorTests
{
    [Test]
    public void Calculate_UsesOnlyLeavesWithinJawAndPositiveOpenings()
    {
        var mlc = new FakeMlcModel(
            new LeafInformation(-5, 5),
            new LeafInformation(0, 5),
            new LeafInformation(5, 5));

        var snapshot = new FakeFieldData(
            mlc,
            y1InMm: -1,
            y2InMm: 6,
            deltaMu: 1,
            isBeamHold: false,
            bank0Positions: new[] { 0f, -2f, 3f },
            bank1Positions: new[] { 10f, 8f, 1f });

        var result = AverageLeafPairOpeningCalculator.Calculate(new FakeFieldDataCollection(snapshot));

        result.ShouldBe(1.0, 1e-9);
    }

    [Test]
    public void Calculate_DefaultOptions_ExcludeZeroMuAndBeamHold()
    {
        var mlc = new FakeMlcModel(new LeafInformation(0, 5));

        var zeroMu = new FakeFieldData(mlc, -5, 5, 0, false, new[] { 0f }, new[] { 20f });
        var beamHold = new FakeFieldData(mlc, -5, 5, 1, true, new[] { 0f }, new[] { 30f });
        var delivering = new FakeFieldData(mlc, -5, 5, 1, false, new[] { 0f }, new[] { 10f });

        var collection = new FakeFieldDataCollection(zeroMu, beamHold, delivering);

        var result = AverageLeafPairOpeningCalculator.Calculate(collection);

        result.ShouldBe(1.0, 1e-9);
    }

    [Test]
    public void Calculate_IncludeOptions_IncludeZeroMuAndBeamHold()
    {
        var mlc = new FakeMlcModel(new LeafInformation(0, 5));

        var zeroMu = new FakeFieldData(mlc, -5, 5, 0, false, new[] { 0f }, new[] { 20f });
        var beamHold = new FakeFieldData(mlc, -5, 5, 1, true, new[] { 0f }, new[] { 30f });
        var delivering = new FakeFieldData(mlc, -5, 5, 1, false, new[] { 0f }, new[] { 10f });

        var collection = new FakeFieldDataCollection(zeroMu, beamHold, delivering);
        var options = new AverageLeafPairOpeningOptions
        {
            IncludeZeroMu = true,
            IncludeBeamHold = true
        };

        var result = AverageLeafPairOpeningCalculator.Calculate(collection, options);

        result.ShouldBe(2.0, 1e-9);
    }

    [Test]
    public void Calculate_NormalizesJawOrder_WhenY1IsGreaterThanY2()
    {
        var mlc = new FakeMlcModel(new LeafInformation(0, 5));
        var snapshot = new FakeFieldData(
            mlc,
            y1InMm: 10,
            y2InMm: -10,
            deltaMu: 1,
            isBeamHold: false,
            bank0Positions: new[] { -5f },
            bank1Positions: new[] { 10f });

        var result = AverageLeafPairOpeningCalculator.Calculate(new FakeFieldDataCollection(snapshot));

        result.ShouldBe(1.5, 1e-9);
    }

    [Test]
    public void Calculate_ReturnsZero_WhenCollectionIsEmpty()
    {
        var result = AverageLeafPairOpeningCalculator.Calculate(new FakeFieldDataCollection());

        result.ShouldBe(0.0, 1e-9);
    }

    [Test]
    public void Calculate_ReturnsZero_WhenAllLeavesOutsideJaw()
    {
        var mlc = new FakeMlcModel(new LeafInformation(20, 5));
        var snapshot = new FakeFieldData(
            mlc,
            y1InMm: -5,
            y2InMm: 5,
            deltaMu: 1,
            isBeamHold: false,
            bank0Positions: new[] { 0f },
            bank1Positions: new[] { 10f });

        var result = AverageLeafPairOpeningCalculator.Calculate(new FakeFieldDataCollection(snapshot));

        result.ShouldBe(0.0, 1e-9);
    }

    [Test]
    public void Calculate_ReturnsZero_WhenAllOpeningsNegative()
    {
        var mlc = new FakeMlcModel(new LeafInformation(0, 5));
        var snapshot = new FakeFieldData(
            mlc,
            y1InMm: -5,
            y2InMm: 5,
            deltaMu: 1,
            isBeamHold: false,
            bank0Positions: new[] { 5f },
            bank1Positions: new[] { 2f });

        var result = AverageLeafPairOpeningCalculator.Calculate(new FakeFieldDataCollection(snapshot));

        result.ShouldBe(0.0, 1e-9);
    }

    private sealed class FakeFieldDataCollection : List<IFieldData>, IFieldDataCollection
    {
        public FakeFieldDataCollection(params IFieldData[] items) : base(items)
        {
        }
    }

    private sealed class FakeMlcModel : IMLCModel
    {
        private readonly LeafInformation[] _leafInformation;

        public FakeMlcModel(params LeafInformation[] leafInformation)
        {
            _leafInformation = leafInformation;
        }

        public LeafInformation GetLeafInformation(int leafIndex) => _leafInformation[leafIndex];

        public int GetNumberOfLeafPairs() => _leafInformation.Length;
    }

    private sealed class FakeFieldData : IFieldData
    {
        private readonly float[] _bank0Positions;
        private readonly float[] _bank1Positions;
        private readonly bool _isBeamHold;

        // bank0 = Bank B (negative X side, index 0 in GetLeafPositionInMm)
        // bank1 = Bank A (positive X side, index 1 in GetLeafPositionInMm)
        // Opening = bank1[i] - bank0[i]
        public FakeFieldData(
            IMLCModel mlc,
            float y1InMm,
            float y2InMm,
            float deltaMu,
            bool isBeamHold,
            float[] bank0Positions,
            float[] bank1Positions)
        {
            Mlc = mlc;
            Y1InMm = y1InMm;
            Y2InMm = y2InMm;
            DeltaMu = deltaMu;
            _isBeamHold = isBeamHold;
            _bank0Positions = bank0Positions;
            _bank1Positions = bank1Positions;
        }

        public IMLCModel Mlc { get; }
        public float X1InMm => 0;
        public float Y1InMm { get; }
        public float X2InMm => 0;
        public float Y2InMm { get; }
        public float GantryInDegrees => 0;
        public float CollimatorInDegrees => 0;
        public float DeltaMu { get; }

        public float GetLeafPositionInMm(int bank, int leafIndex) => bank switch
        {
            0 => _bank0Positions[leafIndex],
            1 => _bank1Positions[leafIndex],
            _ => throw new ArgumentOutOfRangeException(nameof(bank), bank, "Expected bank 0 or 1.")
        };

        public bool IsBeamHold() => _isBeamHold;
    }
}
