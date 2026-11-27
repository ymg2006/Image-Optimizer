using System;
using System.Collections.Generic;
using System.IO;

// Executed under each native tool's filename to record the arguments Windows
// delivered to Main and simulate successful or failed compression.
static class CommandRecorder
{
    static int Main(string[] args)
    {
        string executable = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string directory = Path.GetDirectoryName(executable);
        string tool = Path.GetFileName(executable);
        string input = args[args.Length - 1];
        string output = null;
        if (tool == "pingo.exe") output = input;
        else if (tool == "gifsicle.exe")
        {
            foreach (string argument in args)
                if (argument.StartsWith("--output=")) output = argument.Substring(9);
        }
        else
        {
            int index = Array.IndexOf(args, "-outfile");
            if (index >= 0 && index + 1 < args.Length) output = args[index + 1];
        }

        var record = new List<string> {
            executable, Environment.CurrentDirectory,
            File.Exists(input) ? Convert.ToBase64String(File.ReadAllBytes(input)) : "missing input"
        };
        record.AddRange(args);
        File.WriteAllLines(Path.Combine(directory, "records", Guid.NewGuid().ToString("N") + ".txt"), record);
        if (output == null)
        {
            Console.Error.WriteLine("The command did not specify an output file.");
            return 24;
        }

        string behavior = File.ReadAllText(Path.Combine(directory, "behavior.txt")).Trim();
        if (behavior == "missing")
        {
            if (File.Exists(output)) File.Delete(output);
            return 0;
        }
        if (behavior == "empty")
        {
            File.WriteAllBytes(output, new byte[0]);
            return 0;
        }
        if (behavior == "failure")
        {
            File.WriteAllText(output, "partial output from a failed tool");
            Console.Error.WriteLine("Simulated encoder failure.");
            return 23;
        }
        File.WriteAllText(output, "optimized with " + tool);
        return 0;
    }
}
