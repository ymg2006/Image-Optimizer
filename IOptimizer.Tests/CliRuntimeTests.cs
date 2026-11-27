using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace IOptimizer.Tests
{
    [Trait("Category", "Integration")]
    public sealed class CliRuntimeTests
    {
        private static string Executable => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IOptimizer.exe");

        public static IEnumerable<object[]> CorruptImages()
        {
            foreach (string extension in ImageTestData.ExtensionCases)
                foreach (bool lossy in new[] { false, true })
                    yield return new object[] { extension, lossy };
        }

        [Theory]
        [MemberData(nameof(CorruptImages))]
        public void Corrupt_image_reports_a_normal_cli_error(string extension, bool lossy)
        {
            using (var workspace = new TestWorkspace())
            {
                string source = Path.Combine(workspace.Root, "corrupt" + extension);
                File.WriteAllText(source, "original invalid image bytes");
                ProcessResult result = ProcessRunner.Run(Executable,
                    lossy ? new[] { source, "--lossy" } : new[] { source }, workspace.Root);
                Assert.Equal(1, result.ExitCode);
                Assert.Contains("Could not optimize", result.Output);
                Assert.DoesNotContain("Unhandled Exception", result.Output);
                Assert.DoesNotContain("Optimized corrupt", result.Output);
                Assert.Equal("original invalid image bytes", File.ReadAllText(source));
            }
        }

        [Theory]
        [InlineData("missing.png")]
        [InlineData("missing-folder/image.jpeg")]
        public void Missing_input_reports_a_normal_cli_error(string name)
        {
            using (var workspace = new TestWorkspace())
            {
                string source = Path.Combine(workspace.Root, name);
                ProcessResult result = ProcessRunner.Run(Executable, new[] { source }, workspace.Root);
                Assert.Equal(1, result.ExitCode);
                Assert.Contains("Could not optimize", result.Output);
                Assert.DoesNotContain("Unhandled Exception", result.Output);
                Assert.False(File.Exists(source));
            }
        }

        [Fact]
        public void Directory_without_filter_reports_a_normal_cli_error()
        {
            using (var workspace = new TestWorkspace())
            {
                ProcessResult result = ProcessRunner.Run(Executable, new[] { workspace.Root }, workspace.Root);
                Assert.Equal(1, result.ExitCode);
                Assert.Contains("you must provide a file filter", result.Output);
                Assert.DoesNotContain("Unhandled Exception", result.Output);
            }
        }
    }
}
