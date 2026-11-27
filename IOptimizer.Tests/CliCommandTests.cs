using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace IOptimizer.Tests
{
    [Trait("Category", "Commands")]
    public sealed class CliCommandTests : IClassFixture<CommandTestFixture>
    {
        private readonly CommandTestFixture fixture;
        public CliCommandTests(CommandTestFixture fixture) => this.fixture = fixture;

        public static IEnumerable<object[]> CommandCases()
        {
            foreach (string extension in ImageTestData.ExtensionCases)
                foreach (string flag in new[] { "", "-l", "--lossy" })
                    yield return new object[] { extension, flag };
        }

        public static IEnumerable<object[]> FailureCases()
        {
            foreach (string extension in ImageTestData.Extensions)
                foreach (bool lossy in new[] { false, true })
                    foreach (string behavior in new[] { "failure", "missing", "empty" })
                        yield return new object[] { extension, lossy, behavior };
        }

        public static IEnumerable<object[]> InputOptionCases()
        {
            foreach (string extension in ImageTestData.ExtensionCases)
                foreach (bool lossy in new[] { false, true })
                    foreach (string option in new[] { "-i", "--input" })
                        yield return new object[] { extension, lossy, option };
        }

        [Theory]
        [MemberData(nameof(InputOptionCases))]
        public void Input_options_execute_the_expected_command(string extension, bool lossy, string option)
        {
            using (var scenario = new CommandScenario(fixture))
            {
                string source = scenario.WriteInput("explicit" + extension);
                var arguments = new List<string> { option, source };
                if (lossy) arguments.Add("--lossy");
                ProcessResult result = scenario.Run(arguments.ToArray());
                Assert.Equal(0, result.ExitCode);
                CommandRecord command = scenario.ReadCommand();
                scenario.AssertCommand(command, source, lossy);
                Assert.Equal("optimized with " + command.Tool, File.ReadAllText(source));
            }
        }

        [Theory]
        [InlineData("--help", "--input")]
        [InlineData("--version", "Image Optimizer")]
        public void Parser_information_options_do_not_start_tools(string option, string expected)
        {
            using (var scenario = new CommandScenario(fixture))
            {
                ProcessResult result = scenario.Run(option);
                Assert.Equal(0, result.ExitCode);
                Assert.Contains(expected, result.Output);
                scenario.AssertNoCommands();
            }
        }

        [Fact]
        public void Missing_input_is_reported_without_starting_a_tool()
        {
            using (var scenario = new CommandScenario(fixture))
            {
                ProcessResult result = scenario.Run("--lossy");
                Assert.Equal(1, result.ExitCode);
                Assert.Contains("You must specify a file or folder", result.Output);
                scenario.AssertNoCommands();
            }
        }

        [Fact]
        public void Conflicting_input_paths_are_rejected_without_starting_a_tool()
        {
            using (var scenario = new CommandScenario(fixture))
            {
                string first = scenario.WriteInput("first.png");
                string second = scenario.WriteInput("second.png");
                ProcessResult result = scenario.Run(first, "--input", second);
                Assert.Equal(1, result.ExitCode);
                Assert.Contains("Specify one input path", result.Output);
                scenario.AssertNoCommands();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Options_can_precede_positional_input(bool lossy)
        {
            using (var scenario = new CommandScenario(fixture))
            {
                string source = scenario.WriteInput("options-first.png");
                ProcessResult result = scenario.Run(lossy ? "--lossy" : "--backup", source);
                Assert.Equal(0, result.ExitCode);
                CommandRecord command = scenario.ReadCommand();
                scenario.AssertCommand(command, source, lossy);
                Assert.Equal("optimized with " + command.Tool, File.ReadAllText(source));
            }
        }

        [Fact]
        public void No_arguments_displays_help_without_starting_a_tool()
        {
            using (var scenario = new CommandScenario(fixture))
            {
                ProcessResult result = scenario.Run();
                Assert.Equal(0, result.ExitCode);
                Assert.Contains("--input", result.Output);
                scenario.AssertNoCommands();
            }
        }

        [Theory]
        [InlineData("--unknown-option")]
        [InlineData("--filter")]
        public void Invalid_arguments_fail_without_starting_a_tool(string argument)
        {
            using (var scenario = new CommandScenario(fixture))
            {
                ProcessResult result = scenario.Run(argument);
                Assert.Equal(1, result.ExitCode);
                scenario.AssertNoCommands();
                Assert.DoesNotContain("Unhandled Exception", result.Output);
            }
        }

        [Fact]
        public void Missing_tool_is_reported_without_crashing_or_changing_input()
        {
            using (var scenario = new CommandScenario(fixture))
            {
                string source = scenario.WriteInput("missing-tool.png");
                scenario.RemoveTool("pingo.exe");
                ProcessResult result = scenario.Run(source);
                Assert.Equal(1, result.ExitCode);
                Assert.Contains("The image tool was not found:", result.Output);
                Assert.Contains("pingo.exe", result.Output);
                Assert.DoesNotContain("Unhandled Exception", result.Output);
                Assert.Equal("original fixture bytes", File.ReadAllText(source));
                scenario.AssertNoCommands();
            }
        }

        [Theory]
        [MemberData(nameof(CommandCases))]
        public void Executes_expected_command_and_replaces_original(string extension, string flag)
        {
            using (var scenario = new CommandScenario(fixture))
            {
                string source = scenario.WriteInput("sample" + extension);
                string inputBytes = Convert.ToBase64String(File.ReadAllBytes(source));
                ProcessResult result = flag == "" ? scenario.Run(source) : scenario.Run(source, flag);
                Assert.Equal(0, result.ExitCode);
                CommandRecord command = scenario.ReadCommand();
                scenario.AssertCommand(command, source, flag != "");
                Assert.Equal(inputBytes, command.InputBytes);
                Assert.Equal("optimized with " + command.Tool, File.ReadAllText(source));
                Assert.False(File.Exists(command.Output));
                Assert.False(File.Exists(source + ".bkp"));
                Assert.Contains("Optimized sample", result.Output);
                Assert.Contains("Savings:", result.Output);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Resolves_relative_input_before_executing_tool(bool lossy)
        {
            using (var scenario = new CommandScenario(fixture))
            {
                string source = scenario.WriteInput("relative.jpg");
                ProcessResult result = lossy ? scenario.Run("relative.jpg", "-l") : scenario.Run("relative.jpg");
                Assert.Equal(0, result.ExitCode);
                CommandRecord command = scenario.ReadCommand();
                scenario.AssertCommand(command, source, lossy);
                Assert.Equal("optimized with " + command.Tool, File.ReadAllText(source));
            }
        }

        [Theory]
        [InlineData("-b")]
        [InlineData("--backup")]
        public void Backup_flags_preserve_existing_backups_and_do_not_reach_tool(string flag)
        {
            using (var scenario = new CommandScenario(fixture))
            {
                string source = scenario.WriteInput("backup.png");
                for (int index = 0; index < 2; index++)
                {
                    scenario.Reset();
                    string before = File.ReadAllText(source);
                    ProcessResult result = scenario.Run(source, flag, "--lossy");
                    Assert.Equal(0, result.ExitCode);
                    CommandRecord command = scenario.ReadCommand();
                    scenario.AssertCommand(command, source, true);
                    Assert.Equal(before, File.ReadAllText(source + (index == 0 ? ".bkp" : ".bkp1")));
                    Assert.Equal("original fixture bytes", File.ReadAllText(source + ".bkp"));
                }
            }
        }

        [Theory]
        [MemberData(nameof(FailureCases))]
        public void Failed_commands_preserve_original_and_clean_temporary_output(string extension, bool lossy, string behavior)
        {
            using (var scenario = new CommandScenario(fixture))
            {
                string source = scenario.WriteInput("failure" + extension);
                scenario.Reset(behavior);
                // Directory mode reports the tool error without an unhandled exception.
                var arguments = new List<string> { scenario.FixturesRoot, "-f", "failure" + extension };
                if (lossy) arguments.Add("--lossy");
                ProcessResult result = scenario.Run(arguments.ToArray());
                CommandRecord command = scenario.ReadCommand();
                scenario.AssertCommand(command, source, lossy);
                Assert.Equal(1, result.ExitCode);
                Assert.Equal("original fixture bytes", File.ReadAllText(source));
                Assert.False(File.Exists(command.Output));
                Assert.DoesNotContain("Optimized failure", result.Output);
                if (behavior == "failure")
                {
                    Assert.Contains("exit code 23", result.Output);
                    Assert.Contains("Simulated encoder failure.", result.Output);
                }
                else Assert.Contains("did not produce an optimized image", result.Output);
            }
        }
    }
}
