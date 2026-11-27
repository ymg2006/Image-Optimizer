# Image-Optimizer
<pre>
Image optimizer executable for use with <a href="https://marketplace.visualstudio.com/items?itemName=fabiospampinato.vscode-optimize-images">Optimize Images</a> or the Windows Command Prompt.

Configuration for Optimize Images in VS Code:
    "optimizeImages.app": "C:\\Path to\\IOptimizer\\IOptimizer.exe",
    "optimizeImages.appOptions": ["[filepath]" , "-b"],
    "optimizeImages.searchIgnoreFolders": [".git"],

Application arguments:
You can specify a file or folder immediately after the command, for example: IOptimizer.exe "src" -f *.png
  -i, --input        Set the input file or folder to compress. For a folder, specify a filter with -f.
  -f, --filter       Set the file filter for a folder, for example: -f *.png.
  -r, --recursive    (Default: false) Search all subfolders recursively.
  -l, --lossy        (Default: false) Use lossy compression.
  -b, --backup       (Default: false) Back up original files before replacing them.
  --help             Display this help screen.
  --version          Display version information.
Backups use the original filename followed by .bkp. If that file already exists,
a number is appended: image.png.bkp1, image.png.bkp2, and so on.
 </pre>

The tools have been updated for 64-bit Windows 10 or later. See the
[tool versions, command options, build instructions, and license notes](IOptimizer/Tools/README.md).
Supported image extensions are `.png`, `.jpg`, `.jpeg`, and `.gif`, regardless of case.

The solution includes the `IOptimizer.Tests` xUnit project, targeting .NET
Framework 4.8. Run it on Windows using Visual Studio Test Explorer or:

```powershell
dotnet test "Image Optimizer.sln" --configuration Release
```

Tests run the CLI against recording stand-ins for the native tools and check
the executable, every argument, working directory, path quoting, backup flags,
and failed commands or missing/empty outputs. Integration tests also run the
bundled tools on real PNG, JPEG, and animated GIF fixtures and verify lossless
pixels, animation timing, and executable checksums. Tests run in AppVeyor.
Use `--filter Category=Commands` to run only the CLI command tests.
Real CLI integration tests cover every supported extension in lowercase and
uppercase, both compression modes, and positional, `-i`, and `--input` paths.
Runtime regression tests cover missing files/tools, corrupt images, invalid
arguments, and missing folder filters. Errors print to stderr and return exit
code 1; help and version requests return exit code 0.

Sample input images are included in [IOptimizer.Tests/TestFiles](IOptimizer.Tests/TestFiles).
After running integration tests, open
`IOptimizer.Tests/TestResults/Comparisons/index.html` for an original/lossless/lossy
comparison with sizes, savings, and pixel changes. The report folder also keeps
the actual output images and a `results.csv` file. Generated results are ignored
by Git and remain available after the tests finish.

NuGet restores the .NET Framework reference assemblies needed to build the
existing .NET 4.0 application and the test project. Running the tests requires
.NET Framework 4.8 and a .NET SDK on Windows. The application still targets .NET
Framework 4.0 and now uses CommandLineParser 2.9.1.
