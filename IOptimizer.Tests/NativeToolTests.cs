using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using Xunit;

namespace IOptimizer.Tests
{
    [Trait("Category", "Integration")]
    public sealed class NativeToolTests : IClassFixture<ComparisonReport>
    {
        private static string ToolsRoot => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
        private readonly ComparisonReport report;

        public NativeToolTests(ComparisonReport report) => this.report = report;

        public static IEnumerable<object[]> CompressionCases()
        {
            foreach (string name in ImageTestData.FixtureNames)
                foreach (bool lossy in new[] { false, true })
                    yield return new object[] { name, lossy };
        }

        [Theory]
        [MemberData(nameof(CompressionCases))]
        public void Real_tools_produce_decodable_images_and_preserve_lossless_pixels(string name, bool lossy)
        {
            using (var workspace = new TestWorkspace())
            {
                string source = CopyFixture(workspace.Root, name);
                string originalHash = Hash(source);
                ImageSnapshot before = ImageSnapshot.Read(source);
                CompressionResult result = new Compressor(ToolsRoot).CompressFile(source, lossy);
                try
                {
                    ImageSnapshot after = ImageSnapshot.Read(result.ResultFileName);
                    int changed = before.Pixels.Zip(after.Pixels, (original, optimized) => original != optimized).Count(different => different);
                    report.Add(name, source, lossy, result, changed, before.Pixels.Length);
                    Assert.Equal(before.Width, after.Width);
                    Assert.Equal(before.Height, after.Height);
                    Assert.Equal(before.Frames, after.Frames);
                    Assert.Equal(before.Animation, after.Animation);
                    if (!lossy) Assert.Equal(before.Pixels, after.Pixels);
                    Assert.Equal(originalHash, Hash(source));
                    Assert.Contains("Savings:", result.ToString());
                    Assert.True(result.ResultFileSize > 0);
                }
                finally { File.Delete(result.ResultFileName); }
            }
        }

        public static IEnumerable<object[]> CorruptInputCases()
        {
            foreach (string extension in ImageTestData.ExtensionCases)
                foreach (bool lossy in new[] { false, true })
                    yield return new object[] { extension, lossy };
        }

        [Theory]
        [MemberData(nameof(CorruptInputCases))]
        public void Real_tools_reject_corrupt_input_without_modifying_original(string extension, bool lossy)
        {
            using (var workspace = new TestWorkspace())
            {
                string source = Path.Combine(workspace.Root, "corrupt" + extension);
                File.WriteAllText(source, "This is not an image.");
                string before = Hash(source);
                Assert.Throws<InvalidOperationException>(() => new Compressor(ToolsRoot).CompressFile(source, lossy));
                Assert.Equal(before, Hash(source));
            }
        }

        [Fact]
        public void Missing_native_tool_leaves_original_untouched()
        {
            using (var workspace = new TestWorkspace())
            {
                string source = CopyFixture(workspace.Root, "transparency.png");
                string before = Hash(source);
                var error = Assert.Throws<FileNotFoundException>(() => new Compressor(workspace.Root).CompressFile(source, false));
                Assert.Contains("pingo.exe", error.Message);
                Assert.Equal(before, Hash(source));
            }
        }

        [Fact]
        public void Bundled_executables_match_recorded_checksums()
        {
            ToolManifest manifest;
            using (var stream = File.OpenRead(Path.Combine(ToolsRoot, "versions.json")))
                manifest = (ToolManifest)new DataContractJsonSerializer(typeof(ToolManifest)).ReadObject(stream);
            Assert.NotEmpty(manifest.Tools);
            foreach (ToolFile file in manifest.Tools.SelectMany(tool => tool.Files))
                Assert.True(string.Equals(file.Sha256, Hash(Path.Combine(ToolsRoot, file.Name)), StringComparison.OrdinalIgnoreCase),
                    "Checksum mismatch: " + file.Name);
        }

        private static string CopyFixture(string directory, string name)
        {
            string source = Path.Combine(directory, "sample & %PATH% (\u00e6)" + Path.GetExtension(name));
            File.Copy(ImageTestData.FixturePath(name), source);
            return source;
        }

        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        [DataContract]
        private sealed class ToolManifest
        {
            [DataMember(Name = "tools")] public List<ToolVersion> Tools { get; set; }
        }

        [DataContract]
        private sealed class ToolVersion
        {
            [DataMember(Name = "files")] public List<ToolFile> Files { get; set; }
        }

        [DataContract]
        private sealed class ToolFile
        {
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "sha256")] public string Sha256 { get; set; }
        }
    }
}
