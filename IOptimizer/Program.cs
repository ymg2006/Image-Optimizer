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
            [Option('i', "input", Required = false, HelpText = "Set input file or folder to compress, if you input folder you must specify with -f option.")]
            public string input { get; set; }

            [Option('f', "filter", Required = false, HelpText = "Set filter to search in folder , example: -f *.png")]
            public string filter { get; set; }

            [Option('r', "recursive", Default = false, Required = false, HelpText = "Search recursively all sub folders")]
            public bool recursive { get; set; }

            [Option('l', "lossy", Default = false, Required = false, HelpText = "Convert lossy or not")]
            public bool lossy { get; set; }

            [Option('b', "backup", Default = false, Required = false, HelpText = "Whether to backup original files or not")]
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
                    if (string.IsNullOrEmpty(opts.input))
                    {
                        opts.input = args[0];
                    }
                    else
                    {
                        Console.WriteLine("You mus specify file or directory either by -i or directly after command");
                        return;
                    }
                    opts.input = (opts.input.StartsWith("/")) ? opts.input.Substring(1) : opts.input;
                    if (File.GetAttributes(opts.input).HasFlag(FileAttributes.Directory))
                    {
                        if (string.IsNullOrEmpty(opts.filter))
                        {
                            Console.WriteLine($"When specifying directory, filter argument is mandatory: -f {opts.filter}");
                            return;
                        }
                        else
                        {
                            try
                            {
                                files = new DirectoryInfo(opts.input).GetFiles(opts.filter, opts.recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                                foreach (FileInfo file in files)
                                {
                                    Convert(file, opts.lossy, opts.backup);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message.ToString());
                                return;
                            }
                        }
                    }
                    else
                    {
                        Convert(new FileInfo(opts.input), opts.lossy, opts.backup);
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
                    if (backup) File.Copy(result.OriginalFileName, result.OriginalFileName + ".bkp");
                    File.Copy(result.ResultFileName, result.OriginalFileName, true);
                    File.Delete(result.ResultFileName);
                    Console.WriteLine(result.ToString());
                }
            }
            else
            {
                Console.WriteLine("Not supported format");
                return;
            }
        }
    }
}
