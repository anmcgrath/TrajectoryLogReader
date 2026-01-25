using System.Runtime.InteropServices;
using TrajectoryLogReader.Fluence;

namespace TrajectoryLogReader.Tests.Serialization;

/// <summary>
/// Binary serializer for FieldFluence objects for testing purposes.
/// Format: Header + Grid Data + Options + Jaw Outlines
/// </summary>
public static class FluenceSerializer
{
    private const int Version = 1;

    /// <summary>
    /// Serializes a FieldFluence to a byte array.
    /// </summary>
    public static byte[] Serialize(FieldFluence fluence)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Version
        writer.Write(Version);

        // Grid bounds
        writer.Write(fluence.Grid.Bounds.X);
        writer.Write(fluence.Grid.Bounds.Y);
        writer.Write(fluence.Grid.Bounds.Width);
        writer.Write(fluence.Grid.Bounds.Height);

        // Grid dimensions
        writer.Write(fluence.Grid.Cols);
        writer.Write(fluence.Grid.Rows);

        // Grid data (write as raw bytes for efficiency)
        var dataBytes = MemoryMarshal.AsBytes(fluence.Grid.Data.AsSpan());
        writer.Write(dataBytes.Length);
        writer.Write(dataBytes);

        // Options
        writer.Write(fluence.Options.Cols);
        writer.Write(fluence.Options.Rows);
        writer.Write(fluence.Options.Width);
        writer.Write(fluence.Options.Height);
        writer.Write(fluence.Options.UseApproximateFluence);
        writer.Write(fluence.Options.MinDeltaMu);
        writer.Write(fluence.Options.MaxParallelism);

        return ms.ToArray();
    }

    /// <summary>
    /// Serializes a FieldFluence to a file.
    /// </summary>
    public static void SerializeToFile(FieldFluence fluence, string filePath)
    {
        Console.WriteLine($"Writing {filePath}");
        var bytes = Serialize(fluence);
        Console.WriteLine($"Writing {bytes.Length} bytes");
        File.WriteAllBytes(filePath, bytes);
        Console.WriteLine($"Wrote {filePath}");
        Console.WriteLine($"File is {File.ReadAllText(filePath).Length} bytes");
    }

    /// <summary>
    /// Deserializes a FieldFluence from a byte array.
    /// </summary>
    public static FieldFluence Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // Version
        var version = reader.ReadInt32();
        if (version != Version)
            throw new InvalidOperationException($"Unsupported fluence file version: {version}");

        // Grid bounds
        var boundsX = reader.ReadDouble();
        var boundsY = reader.ReadDouble();
        var boundsWidth = reader.ReadDouble();
        var boundsHeight = reader.ReadDouble();

        // Grid dimensions
        var cols = reader.ReadInt32();
        var rows = reader.ReadInt32();

        // Grid data
        var dataLength = reader.ReadInt32();
        var dataBytes = reader.ReadBytes(dataLength);

        var bounds = new Rect(boundsX, boundsY, boundsWidth, boundsHeight);
        var grid = new GridF(bounds, cols, rows);
        MemoryMarshal.Cast<byte, float>(dataBytes).CopyTo(grid.Data);

        // Options
        var optionsCols = reader.ReadInt32();
        var optionsRows = reader.ReadInt32();
        var optionsWidth = reader.ReadDouble();
        var optionsHeight = reader.ReadDouble();
        var useApproximate = reader.ReadBoolean();
        var minDeltaMu = reader.ReadDouble();
        var maxParallelism = reader.ReadInt32();

        var options = new FluenceOptions(optionsCols, optionsRows)
        {
            Width = optionsWidth,
            Height = optionsHeight,
            UseApproximateFluence = useApproximate,
            MinDeltaMu = minDeltaMu,
            MaxParallelism = maxParallelism
        };


        return new FieldFluence(grid, options, new List<Point[]>());
    }

    /// <summary>
    /// Deserializes a FieldFluence from a file.
    /// </summary>
    public static FieldFluence DeserializeFromFile(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        return Deserialize(bytes);
    }
}