# Image tools

Updated on November 27, 2026. These builds require 64-bit Windows 10 or later
and the Windows Universal C Runtime. Standard project builds copy this entire
directory into the output `Tools` directory.

| Files | Version | Official source |
| --- | --- | --- |
| `pingo.exe` | 1.29 rc3 (the current publisher download; prerelease) | [Pingo](https://css-ig.net/pingo), [changelog](https://css-ig.net/submit/changelog), [Windows download](https://css-ig.net/bin/pingo.zip) |
| `cjpeg.exe`, `jpegtran.exe` | MozJPEG 4.1.1, latest tagged release | [Release](https://github.com/mozilla/mozjpeg/releases/tag/v4.1.1), [source archive](https://github.com/mozilla/mozjpeg/archive/refs/tags/v4.1.1.zip) |
| `gifsicle.exe` | 1.99, latest source release | [Gifsicle](https://www.lcdf.org/gifsicle/), [source archive](https://www.lcdf.org/gifsicle/gifsicle-1.99.tar.gz) |
| `guetzli_windows_x86-64.exe` | 1.0.1, unchanged; matches the latest official binary | [Release](https://github.com/google/guetzli/releases/tag/v1.0.1) |

Guetzli is retained for compatibility but is not invoked by Image Optimizer.
The new MozJPEG executables link the JPEG library statically and do not load
`libjpeg-62.dll`, so that obsolete runtime is no longer bundled.

## Commands used by the application

Paths are passed directly to each executable, without a command shell. PNGs
are copied to a temporary file before Pingo runs because Pingo modifies its
input in place. JPEG and GIF tools write to separate temporary output files.
Nonzero tool exits or missing/empty outputs fail compression and leave the
original file untouched.

| Format | Default | With `--lossy` |
| --- | --- | --- |
| PNG | `pingo -s4 -lossless -noalpha -quiet "temporary.png"` | `pingo -s4 -quality=79 -quiet "temporary.png"` |
| JPEG | `jpegtran -copy none -optimize -progressive -outfile "output.jpg" "input.jpg"` | `cjpeg -quality 80,60 -optimize -progressive -outfile "output.jpg" "input.jpg"` |
| GIF | `gifsicle --optimize=3 --no-ignore-errors --output="output.gif" "input.gif"` | `gifsicle --optimize=3 --no-ignore-errors --lossy=80 --output="output.gif" "input.gif"` |

Pingo's old `-s8`, `-pngpalette`, `-auto`, and `-q` switches have been replaced.
`-noalpha` preserves RGB values under fully transparent pixels in lossless mode.
MozJPEG now uses its default accurate integer DCT and compression tuning,
without the legacy floating-point DCT and input smoothing overrides.
The GIF lossy option now uses Gifsicle's lossy optimizer; previously it was ignored.
PNG/JPEG metadata can still be removed by the optimization commands.

## Native builds

MozJPEG and Gifsicle were built from the source releases above with
[LLVM-MinGW 20261117](https://github.com/mstorsjo/llvm-mingw/releases/tag/20261117)
(`ucrt-x86_64`). The portable compiler's `bin` directory must be on `PATH`.
No compiler runtimes or JPEG DLLs need to be shipped alongside these executables.

MozJPEG was built with CMake 3.31.8, Ninja 1.13.1, and NASM 2.16.03:

```powershell
cmake -S mozjpeg-4.1.1 -B mozjpeg-build -G Ninja `
  -DCMAKE_C_COMPILER=x86_64-w64-mingw32-clang.exe `
  -DCMAKE_BUILD_TYPE=Release -DENABLE_SHARED=OFF -DENABLE_STATIC=ON `
  -DWITH_TURBOJPEG=OFF -DPNG_SUPPORTED=OFF -DREQUIRE_SIMD=ON `
  -DCMAKE_ASM_NASM_COMPILER=nasm.exe
cmake --build mozjpeg-build --target cjpeg-static jpegtran-static
```

Copy `cjpeg-static.exe` and `jpegtran-static.exe` as `cjpeg.exe` and `jpegtran.exe`.
Cjpeg accepts JPEG input; PNG encoding in this application is handled by Pingo,
so this build does not depend on libpng or zlib.

Gifsicle's complete upstream source archive is included in `Sources`.
Extract it, then use the included build script:

```powershell
tar -xzf Sources/gifsicle-1.99.tar.gz -C path/to/workspace
powershell -File Sources/Build-Gifsicle.ps1 `
  -SourcePath path/to/workspace/gifsicle-1.99 `
  -ClangPath path/to/llvm-mingw/bin/x86_64-w64-mingw32-clang.exe `
  -OutputPath path/to/output
```

The script adapts the upstream Windows configuration to recognize MinGW's
standard integer headers. No compression algorithms were modified.

## Verification and licenses

`versions.json` records source-archive and executable SHA-256 hashes. From the
repository root, run `dotnet test "Image Optimizer.sln" --configuration Release`.
The `IOptimizer.Tests` xUnit project verifies the commands executed by the CLI,
real PNG/JPEG/GIF compression, lossless pixels, animation timing, quoted
filenames, and failures on corrupt inputs or missing tools.

The current [Pingo license](https://css-ig.net/pingo) limits use to private use
and prohibits redistribution/repackaging and reuse through other services.
This local update does not grant redistribution rights. Obtain the publisher's
permission before shipping a package containing Pingo or using it outside those
terms. The publisher's library notices are included under `Licenses`.

Gifsicle is GPL-2.0-only; its license, source archive, and build script are
included. Guetzli uses Apache-2.0. MozJPEG's notices are in `Licenses/mozjpeg`.
This software is based in part on the work of the Independent JPEG Group.
