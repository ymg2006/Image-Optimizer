using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IOptimizer
{
    public class Compressor
    {
        static readonly string[] _supported = { ".png", ".jpg", ".jpeg", ".gif" };
        string _cwd;

        public Compressor()
        {
            _cwd = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), @"Tools\");
        }

        public Compressor(string cwd)
        {
            _cwd = Path.GetFullPath(cwd);
        }

        public CompressionResult CompressFile(string fileName, bool lossy)
        {
            fileName = Path.GetFullPath(fileName);
            if (!File.Exists(fileName))
                throw new FileNotFoundException("The input file does not exist.", fileName);
            if (!IsFileSupported(fileName))
                throw new NotSupportedException("Unsupported file format.");

            string targetFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + Path.GetExtension(fileName));
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            string tool = extension == ".png" ? "pingo.exe"
                : extension == ".gif" ? "gifsicle.exe"
                : lossy ? "cjpeg.exe" : "jpegtran.exe";
            string toolPath = Path.Combine(_cwd, tool);
            if (!File.Exists(toolPath))
                throw new FileNotFoundException("The image tool was not found: " + toolPath, toolPath);

            try
            {
                var start = new ProcessStartInfo(toolPath)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = _cwd,
                    Arguments = GetArguments(fileName, targetFile, lossy),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                };

                var stopwatch = Stopwatch.StartNew();
                using (var process = Process.Start(start))
                {
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                        throw new InvalidOperationException(tool + " failed (exit code " + process.ExitCode + "): " + error.Trim());
                }

                stopwatch.Stop();
                if (!File.Exists(targetFile) || new FileInfo(targetFile).Length == 0)
                    throw new InvalidOperationException(tool + " did not produce an optimized image.");

                return new CompressionResult(fileName, targetFile, stopwatch.Elapsed);
            }
            catch
            {
                if (File.Exists(targetFile)) File.Delete(targetFile);
                throw;
            }
        }

        private static string GetArguments(string sourceFile, string targetFile, bool lossy)
        {
            string ext = Path.GetExtension(sourceFile).ToLowerInvariant();
            switch (ext)
            {
                case ".png":
                    File.Copy(sourceFile, targetFile);

                    if (lossy)
                        return string.Format(CultureInfo.InvariantCulture, "-s4 -quality=79 -quiet \"{0}\"", targetFile);
                    else
                        return string.Format(CultureInfo.InvariantCulture, "-s4 -lossless -noalpha -quiet \"{0}\"", targetFile);

                case ".jpg":
                case ".jpeg":
                    if (lossy)
                        return string.Format(CultureInfo.InvariantCulture, "-quality 80,60 -optimize -progressive -outfile \"{1}\" \"{0}\"", sourceFile, targetFile);
                    else
                        return string.Format(CultureInfo.InvariantCulture, "-copy none -optimize -progressive -outfile \"{1}\" \"{0}\"", sourceFile, targetFile);

                case ".gif":
                    return string.Format(CultureInfo.InvariantCulture, "--optimize=3 --no-ignore-errors {2}--output=\"{1}\" \"{0}\"", sourceFile, targetFile, lossy ? "--lossy=80 " : "");
            }

            return null;
        }

        public static bool IsFileSupported(string fileName)
        {
            string ext = Path.GetExtension(fileName);

            return _supported.Any(s => s.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}
