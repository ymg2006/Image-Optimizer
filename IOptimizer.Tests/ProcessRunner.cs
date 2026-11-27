using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOptimizer.Tests
{
    internal sealed class ProcessResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
    }

    internal static class ProcessRunner
    {
        public static ProcessResult Run(string executable, IEnumerable<string> arguments, string directory)
        {
            var start = new ProcessStartInfo(executable)
            {
                Arguments = string.Join(" ", arguments.Select(Quote)),
                WorkingDirectory = directory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using (var process = Process.Start(start))
            {
                Task<string> output = process.StandardOutput.ReadToEndAsync();
                Task<string> error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(30000))
                {
                    process.Kill();
                    process.WaitForExit();
                    throw new TimeoutException("The test process did not finish: " + executable);
                }
                return new ProcessResult { ExitCode = process.ExitCode, Output = output.Result + error.Result };
            }
        }

        // Windows command-line escaping, including embedded quotes and trailing backslashes.
        private static string Quote(string argument)
        {
            var result = new StringBuilder("\"");
            int backslashes = 0;
            foreach (char character in argument)
            {
                if (character == '\\') { backslashes++; continue; }
                result.Append('\\', character == '"' ? backslashes * 2 + 1 : backslashes);
                result.Append(character);
                backslashes = 0;
            }
            return result.Append('\\', backslashes * 2).Append('"').ToString();
        }
    }
}
