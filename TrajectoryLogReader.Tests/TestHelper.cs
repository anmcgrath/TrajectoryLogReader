using NUnit.Framework;

namespace TrajectoryLogReader.Tests
{
    public static class TestHelper
    {
        /// <summary>
        /// Gets the absolute path to a file in the TestFiles directory.
        /// </summary>
        /// <param name="fileName">The name of the file (e.g., "golden_data.bin")</param>
        /// <returns>The full path to the file.</returns>
        public static string GetTestFilePath(string fileName)
        {
            var testDirectory = TestContext.CurrentContext.TestDirectory;
            return Path.Combine(testDirectory, "TestFiles", fileName);
        }
    }
}
