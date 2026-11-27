using System;
using System.IO;
using CommandLine;

namespace IOptimizer
{
    static class Program
    {
        /// <summary>
        /// The acceptable arguments.
        /// </summary>
        public class Options
        {
            [Value(0, MetaName = "path", Required = false, HelpText = "Input file or folder to compress.")]
            public string path { get; set; }

            [Option('i', "input", Required = false, HelpText = "Set the input file or folder to compress. For a folder, specify a filter with -f.")]
            public string input { get; set; }

            [Option('f', "filter", Required = false, HelpText = "Set the file filter for a folder, for example: -f *.png.")]
            public string filter { get; set; }

            [Option('r', "recursive", Default = false, Required = false, HelpText = "Search all subfolders recursively.")]
            public bool recursive { get; set; }

            [Option('l', "lossy", Default = false, Required = false, HelpText = "Use lossy compression.")]
            public bool lossy { get; set; }

            [Option('b', "backup", Default = false, Required = false, HelpText = "Back up original files before replacing them.")]
            public bool backup { get; set; }
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new[] {"--help"};
            }
            FileInfo[] files;
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts =>
                {
                    if (!string.IsNullOrEmpty(opts.input) && !string.IsNullOrEmpty(opts.path))
                    {
                        Console.Error.WriteLine("Specify one input path, using -i or directly after the command.");
                        Environment.ExitCode = 1;
                        return;
                    }
                    opts.input = string.IsNullOrEmpty(opts.input) ? opts.path : opts.input;
                    if (string.IsNullOrWhiteSpace(opts.input))
                    {
                        Console.Error.WriteLine("You must specify a file or folder using -i or directly after the command.");
                        Environment.ExitCode = 1;
                        return;
                    }
                    try
                    {
                        if (File.GetAttributes(opts.input).HasFlag(FileAttributes.Directory))
                        {
                            if (string.IsNullOrEmpty(opts.filter))
                            {
                                Console.Error.WriteLine("When specifying a folder, you must provide a file filter, for example: -f *.png.");
                                Environment.ExitCode = 1;
                                return;
                            }
                            files = new DirectoryInfo(opts.input).GetFiles(opts.filter, opts.recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                            foreach (FileInfo file in files)
                            {
                                Convert(file, opts.lossy, opts.backup);
                            }
                        }
                        else
                        {
                            Convert(new FileInfo(opts.input), opts.lossy, opts.backup);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Could not optimize \"" + opts.input + "\": " + e.Message);
                        Environment.ExitCode = 1;
                    }
                })
                .WithNotParsed(errors =>
                {
                    foreach (var error in errors)
                    {
                        if (error.Tag != ErrorType.HelpRequestedError && error.Tag != ErrorType.VersionRequestedError)
                            Environment.ExitCode = 1;
                    }
                });
        }

        /// <summary>
        /// The Convert function for the application.
        /// </summary>
        static void Convert(FileInfo file, bool lossy, bool backup)
        {
            Compressor _compressor = new Compressor();
            if (Compressor.IsFileSupported(file.FullName))
            {
                CompressionResult result = _compressor.CompressFile(file.FullName, lossy);

                if (File.Exists(result.ResultFileName))
                {
                    if (backup)
                    {
                        string backupFileName = result.OriginalFileName + ".bkp";
                        int backupNumber = 1;
                        while (File.Exists(backupFileName))
                        {
                            backupFileName = result.OriginalFileName + ".bkp" + backupNumber;
                            backupNumber++;
                        }
                        File.Copy(result.OriginalFileName, backupFileName);
                    }
                    File.Copy(result.ResultFileName, result.OriginalFileName, true);
                    File.Delete(result.ResultFileName);
                    Console.WriteLine(result.ToString());
                }
            }
            else
            {
                Console.Error.WriteLine("Unsupported file format.");
                Environment.ExitCode = 1;
                return;
            }
        }
    }
}
