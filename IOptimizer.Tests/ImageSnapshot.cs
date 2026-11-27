using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace IOptimizer.Tests
{
    internal sealed class ImageSnapshot
    {
        public int Width, Height, Frames;
        public int[] Pixels;
        public string[] Animation;

        public static ImageSnapshot Read(string path)
        {
            using (var image = (Bitmap)Image.FromFile(path))
            {
                var dimension = new FrameDimension(image.FrameDimensionsList[0]);
                int frames = image.GetFrameCount(dimension);
                var pixels = new List<int>();
                for (int frame = 0; frame < frames; frame++)
                {
                    image.SelectActiveFrame(dimension, frame);
                    for (int y = 0; y < image.Height; y++)
                        for (int x = 0; x < image.Width; x++) pixels.Add(image.GetPixel(x, y).ToArgb());
                }
                return new ImageSnapshot {
                    Width = image.Width, Height = image.Height, Frames = frames, Pixels = pixels.ToArray(),
                    Animation = image.PropertyItems.Where(item => item.Id == 0x5100 || item.Id == 0x5101)
                        .OrderBy(item => item.Id).Select(item => item.Id + ":" + Convert.ToBase64String(item.Value)).ToArray(),
                };
            }
        }
    }
}
