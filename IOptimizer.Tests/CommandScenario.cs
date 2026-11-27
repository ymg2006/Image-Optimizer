using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace IOptimizer.Tests
{
    internal sealed class CommandRecord
    {
        public string Executable { get; set; }
        public string WorkingDirectory { get; set; }
        public string InputBytes { get; set; }
        public string[] Arguments { get; set; }
        public string Output { get; set; }
        public string Tool => Path.GetFileName(Executable);
    }

    internal sealed class CommandScenario : IDisposable
    {
        private readonly TestWorkspace workspace = new TestWorkspace();
        private readonly HashSet<string> outputs = new HashSet<string>();
        private readonly string cliPath;
        private readonly string recordsRoot;
        public string ToolsRoot { get; }
        public string FixturesRoot { get; }

        public CommandScenario(CommandTestFixture fixture)
        {
            string appRoot = Path.Combine(workspace.Root, "CLI & %PATH% ! ^ ( ) [\u00e6]");
            ToolsRoot = Path.Combine(appRoot, "Tools");
            FixturesRoot = Path.Combine(workspace.Root, "images & %PATH% ! ^ ( ) [\u00e6]");
            recordsRoot = Path.Combine(ToolsRoot, "records");
            Directory.CreateDirectory(recordsRoot);
            Directory.CreateDirectory(FixturesRoot);
            cliPath = Path.Combine(appRoot, "IOptimizer.exe");
            File.Copy(typeof(Compressor).Assembly.Location, cliPath);
            File.Copy(typeof(CommandLine.Parser).Assembly.Location, Path.Combine(appRoot, "CommandLine.dll"));
            foreach (string tool in new[] { "pingo.exe", "cjpeg.exe", "jpegtran.exe", "gifsicle.exe" })
                File.Copy(fixture.RecorderPath, Path.Combine(ToolsRoot, tool));
            Reset();
        }

        public string WriteInput(string name, string contents = "original fixture bytes")
        {
            string path = Path.Combine(FixturesRoot, name);
            File.WriteAllText(path, contents);
            return path;
        }

        public void Reset(string behavior = "success")
        {
            foreach (string file in Directory.GetFiles(recordsRoot)) File.Delete(file);
            File.WriteAllText(Path.Combine(ToolsRoot, "behavior.txt"), behavior);
        }

        public ProcessResult Run(params string[] arguments) => ProcessRunner.Run(cliPath, arguments, FixturesRoot);

        public void AssertNoCommands() => Assert.Empty(Directory.GetFiles(recordsRoot));

        public void RemoveTool(string name) => File.Delete(Path.Combine(ToolsRoot, name));

        public CommandRecord ReadCommand()
        {
            string recordPath = Assert.Single(Directory.GetFiles(recordsRoot));
            string[] lines = File.ReadAllLines(recordPath);
            string[] arguments = lines.Skip(3).ToArray();
            string tool = Path.GetFileName(lines[0]);
            string output = tool == "pingo.exe" ? arguments.Last()
                : tool == "gifsicle.exe" ? Assert.Single(arguments, arg => arg.StartsWith("--output=")).Substring(9)
                : arguments[Array.IndexOf(arguments, "-outfile") + 1];
            outputs.Add(output);
            return new CommandRecord {
                Executable = lines[0], WorkingDirectory = lines[1], InputBytes = lines[2],
                Arguments = arguments, Output = output,
            };
        }

        public void AssertCommand(CommandRecord record, string source, bool lossy)
        {
            string tool;
            var expected = new List<string>();
            switch (Path.GetExtension(source).ToLowerInvariant())
            {
                case ".png":
                    tool = "pingo.exe";
                    expected.AddRange(lossy ? new[] { "-s4", "-quality=79", "-quiet" }
                        : new[] { "-s4", "-lossless", "-noalpha", "-quiet" });
                    expected.Add(record.Output);
                    break;
                case ".jpg":
                case ".jpeg":
                    tool = lossy ? "cjpeg.exe" : "jpegtran.exe";
                    expected.AddRange(lossy ? new[] { "-quality", "80,60", "-optimize", "-progressive" }
                        : new[] { "-copy", "none", "-optimize", "-progressive" });
                    expected.AddRange(new[] { "-outfile", record.Output, source });
                    break;
                case ".gif":
                    tool = "gifsicle.exe";
                    expected.AddRange(new[] { "--optimize=3", "--no-ignore-errors" });
                    if (lossy) expected.Add("--lossy=80");
                    expected.AddRange(new[] { "--output=" + record.Output, source });
                    break;
                default: throw new ArgumentException("Unsupported test format.", nameof(source));
            }
            Assert.Equal(Path.Combine(ToolsRoot, tool), record.Executable);
            Assert.Equal(ToolsRoot, record.WorkingDirectory);
            Assert.Equal(expected, record.Arguments);
            Assert.Equal(Path.GetExtension(source), Path.GetExtension(record.Output));
            Assert.Equal(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(record.Output));
            Assert.NotEqual(source, record.Output);
        }

        public void Dispose()
        {
            foreach (string output in outputs) if (File.Exists(output)) File.Delete(output);
            workspace.Dispose();
        }
    }
}
