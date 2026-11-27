using System;
using System.IO;

namespace IOptimizer.Tests
{
    public sealed class TestWorkspace : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "image-optimizer-tests-" + Guid.NewGuid().ToString("N"));

        public TestWorkspace()
        {
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            string resolved = Path.GetFullPath(Root);
            string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(resolved).StartsWith("image-optimizer-tests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to clean a directory outside the test workspace.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
    }
}
