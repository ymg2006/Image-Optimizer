# Image fixtures

These synthetic fixtures belong to this project and are safe to modify in test
workspaces. Integration tests copy these files before running the native tools;
the originals in this directory are never overwritten.
Every supported extension (`.png`, `.jpg`, `.jpeg`, `.gif`) is exercised through
the real CLI with lowercase/uppercase filenames, lossless/lossy compression,
and positional, `-i`, and `--input` arguments.

- `color-chart.png`: gradients, sharp color boundaries, fine lines, and text.
- `transparency.png`: opaque, partially transparent, and fully transparent
  pixels, including nonzero RGB data under zero alpha.
- `landscape.jpg`: a detailed synthetic landscape encoded at JPEG quality 95.
- `landscape.jpeg`: the same JPEG bytes with the alternative supported extension.
- `animation.gif`: four frames with different delays, a finite loop count, and
  moving colored shapes.

Run the integration tests from the repository root:

```powershell
dotnet test "Image Optimizer.sln" --configuration Release --filter Category=Integration
```

Open `IOptimizer.Tests/TestResults/Comparisons/index.html` after the run to see
the original, lossless, and lossy images together with file sizes, savings, and
decoded pixel changes. The same folder contains `results.csv` and the actual
image files in `original`, `lossless`, and `lossy` subdirectories.

These generated results remain on disk for inspection and are ignored by Git.
Running the tests again replaces results for the fixtures being tested. The
report only includes cases generated in the current run; these outputs are
inspection artifacts rather than fixed expected files that must match byte for
byte across tool versions.
