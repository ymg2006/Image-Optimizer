using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IOptimizer.Tests
{
    internal static class ImageTestData
    {
        public static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".gif" };
        public static readonly string[] FixtureNames = {
            "color-chart.png", "transparency.png", "landscape.jpg", "landscape.jpeg", "animation.gif"
        };

        public static IEnumerable<string> ExtensionCases => Extensions.Concat(Extensions.Select(extension => extension.ToUpperInvariant()));

        public static string FixturePath(string name) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", name);

        public static string FixtureFor(string extension)
        {
            return FixturePath(FixtureNames.First(name => Path.GetExtension(name).Equals(extension, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
