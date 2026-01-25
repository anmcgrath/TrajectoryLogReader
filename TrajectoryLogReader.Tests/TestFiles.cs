namespace TrajectoryLogReader.Tests;

public static class TestFiles
{
    public static string Directory { get; } = Path.Combine(AppContext.BaseDirectory, "TestFiles");

    public static string GetPath(string relativePath) => Path.Combine(Directory, relativePath);
}
