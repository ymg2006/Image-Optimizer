using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace IOptimizer.Tests
{
    [Trait("Category", "Integration")]
    public sealed class CliImageTests
    {
        public static IEnumerable<object[]> ImageCases()
        {
            foreach (string extension in ImageTestData.ExtensionCases)
                foreach (bool lossy in new[] { false, true })
                    foreach (string inputOption in new[] { "", "-i", "--input" })
                        yield return new object[] { extension, lossy, inputOption };
        }

        [Theory]
        [MemberData(nameof(ImageCases))]
        public void Real_cli_optimizes_each_supported_type(string extension, bool lossy, string inputOption)
        {
            using (var workspace = new TestWorkspace())
            {
                string source = Path.Combine(workspace.Root, "real image & %PATH% [\u00e6]" + extension);
                File.Copy(ImageTestData.FixtureFor(extension), source);
                ImageSnapshot before = ImageSnapshot.Read(source);
                var arguments = new List<string>();
                if (inputOption != "") arguments.Add(inputOption);
                arguments.Add(source);
                if (lossy) arguments.Add("--lossy");

                // Assembly.Location may point into xUnit's shadow-copy cache,
                // where the CLI's dependency DLLs are not in the same folder.
                string executable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IOptimizer.exe");
                Assert.True(File.Exists(executable), "The built CLI is missing: " + executable);
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(executable), "CommandLine.dll")),
                    "The CLI's CommandLineParser dependency is missing.");
                ProcessResult result = ProcessRunner.Run(executable, arguments, workspace.Root);
                Assert.True(result.ExitCode == 0, executable + Environment.NewLine + result.Output);
                Assert.Contains("Optimized " + Path.GetFileName(source), result.Output);
                Assert.Contains("Savings:", result.Output);
                Assert.False(File.Exists(source + ".bkp"));
                ImageSnapshot after = ImageSnapshot.Read(source);
                Assert.Equal(before.Width, after.Width);
                Assert.Equal(before.Height, after.Height);
                Assert.Equal(before.Frames, after.Frames);
                Assert.Equal(before.Animation, after.Animation);
                if (!lossy) Assert.Equal(before.Pixels, after.Pixels);
            }
        }

        [Fact]
        public void Fixtures_cover_all_supported_extensions()
        {
            string[] fixtureExtensions = ImageTestData.FixtureNames.Select(Path.GetExtension).Distinct().OrderBy(extension => extension).ToArray();
            Assert.Equal(ImageTestData.Extensions.OrderBy(extension => extension), fixtureExtensions);
            foreach (string extension in ImageTestData.ExtensionCases)
            {
                Assert.True(Compressor.IsFileSupported("image" + extension));
                Assert.True(File.Exists(ImageTestData.FixtureFor(extension)));
            }
        }
    }
}
