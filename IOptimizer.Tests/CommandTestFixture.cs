using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Xunit;

namespace IOptimizer.Tests
{
    public sealed class CommandTestFixture : IDisposable
    {
        private readonly TestWorkspace workspace = new TestWorkspace();
        internal string RecorderPath { get; }

        public CommandTestFixture()
        {
            RecorderPath = Path.Combine(workspace.Root, "recorder.exe");
            using (var provider = new CSharpCodeProvider())
            {
                var options = new CompilerParameters(new[] { "System.dll" }, RecorderPath, false)
                {
                    GenerateExecutable = true,
                };
                CompilerResults result = provider.CompileAssemblyFromFile(options,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "CommandRecorder.cs"));
                Assert.False(result.Errors.HasErrors,
                    string.Join(Environment.NewLine, result.Errors.Cast<CompilerError>().Select(error => error.ToString())));
            }
        }

        public void Dispose() => workspace.Dispose();
    }
}
