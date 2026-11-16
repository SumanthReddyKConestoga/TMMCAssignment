# TMMC Vertical Line Counter

A **C# console application for Windows (.NET Framework 4.8)** that counts the vertical black lines in a JPEG image and prints the result.

Give it an image with three separate black bars and it prints `3`. A thick bar counts as **one** line, not one line per black pixel.

## Quick start

From the project root in PowerShell:

```powershell
.\app\TMMC.LineCounter.exe "$PWD\test-images\img_4.jpg"
```

```text
7
```

You need Windows with the .NET Framework 4.8 runtime. Full steps are in [Run on your computer](#run-on-your-computer).

## Contents

- [Requirements of the assignment](#requirements-of-the-assignment)
- [Run on your computer](#run-on-your-computer)
- [Algorithm](#algorithm)
- [How the solution was developed](#how-the-solution-was-developed)
- [Build from source and run tests](#build-from-source-and-run-tests)
- [Errors and troubleshooting](#errors-and-troubleshooting)
- [Limitations](#limitations)
- [Project files](#project-files)

## Requirements of the assignment

This project solves the TMMC programming assignment: count black lines automatically, using an efficient algorithm, readable code, and reliable error handling.

The application must:

- Accept exactly **one argument**: the full path of a JPEG image.
- Print the number of vertical black lines.
- Report wrong arguments, invalid files, and handled exceptions to the console.

The images have a white background and straight, continuous black bars. **Every bar extends through both the top half and the bottom half of the image.** The counting method depends on this guarantee.

## Run on your computer

### 1. Install the runtime

Use Windows with **.NET Framework 4.8**, or a later .NET Framework 4.x runtime. If it is missing, install the **Runtime** from [Microsoft's download page](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48).

You do not need Visual Studio or Git to run the included program. Modern .NET (for example .NET 8 or .NET 9) is a different product and does not replace .NET Framework 4.8.

### 2. Download and extract

Open [this repository](https://github.com/SumanthReddyKConestoga/TMMCAssignment), select **Code > Download ZIP**, and choose **Extract All** in File Explorer.

Open the extracted folder that contains `app`, `src`, `tests`, `test-images`, and `TMMC.LineCounter.sln`. This is the **project root**. Its folder name does not matter.

### 3. Open PowerShell in the project root

Click the File Explorer address bar, type `powershell`, and press **Enter**.

### 4. Run a supplied image

```powershell
.\app\TMMC.LineCounter.exe "$PWD\test-images\img_4.jpg"
```

`$PWD` inserts the full path of the current folder. The quotes keep a path with spaces together as one argument.

Change `img_4.jpg` to another file name to check each result:

| Image | Expected count | Notes |
| --- | ---: | --- |
| `img_1.jpg` | 1 | |
| `img_2.jpg` | 3 | |
| `img_3.jpg` | 1 | Fully black image = one continuous black region |
| `img_4.jpg` | 7 | |

### 5. Run your own image

Replace the example with the full path of your JPEG:

```powershell
.\app\TMMC.LineCounter.exe "C:\Images\my image.jpg"
```

The image must follow the assignment's rules (white background, vertical black bars). The program only reads the file; it never changes it.

> **Use PowerShell, not double-click.** Double-clicking the `.exe` passes no image path, so the program reports wrong usage and closes.

## Algorithm

**Middle-row scan that counts black runs.** A *run* is an unbroken stretch of black pixels.

### The idea

Draw a horizontal line through the middle of the image (row `height / 2`). Every valid bar crosses it:

```text
Middle row:  WHITE | BLACK | WHITE | BLACK | WHITE | BLACK
Count:                 1               2               3
```

The program reads that row from left to right. It adds 1 only when it **enters** a black stretch, and does not add again until white separates it from the next stretch.

```text
count = 0
previousWasBlack = false

for each pixel in the middle row:
    isBlack = (red + green + blue) < 128 * 3
    if isBlack and not previousWasBlack:
        count = count + 1
    previousWasBlack = isBlack

print count
```

### Why it is correct

1. Every bar reaches both the top half and the bottom half, so every bar crosses the middle row.
2. A straight vertical bar crosses that row as **one** unbroken black stretch, however thick it is.
3. Separate bars are separated by white, so they make separate stretches.
4. Counting the **start** of each stretch therefore counts each bar exactly once, including bars touching the left or right edge.

### Handling JPEG shading

JPEG compression turns pure black into dark grey and pure white into light grey. So a pixel is treated as black when its average colour is below the midpoint (`red + green + blue < 384`), not only when it is exactly `0, 0, 0`.

For speed, the selected row is copied into one fixed pixel format and its bytes are read in bulk, instead of calling `GetPixel` once per pixel.

### Efficiency

| Stage | Time | Extra memory |
| --- | --- | --- |
| Counting (one row) | O(width) | O(width) |
| Opening and decoding the JPEG | O(width × height) | O(width × height) |

The counting itself only grows with the number of columns. Decoding the JPEG still touches the whole image; that cost comes from the file format and cannot be avoided by any counting method.

### Other approaches considered

| Approach | Why it was not chosen |
| --- | --- |
| Count all black pixels | Measures black **area**, not the number of bars. |
| Scan the top or bottom row | Misses bars that do not reach the image edge. |
| Scan every row, or sum every column | Works, but reads many more pixels than the guaranteed middle row needs. |
| Connected-component labelling | Good for shapes anywhere in an image, but needs the full image and more bookkeeping. |
| Edge detection + Hough transform | Good for general line shapes, but adds tuning parameters and sees each thick bar as **two** edges. |

The middle-row method was chosen because it follows directly from the requirements and has a short, clear correctness argument. This is a design comparison, not a benchmark. The method fits these images; it is not a general solution for any picture.

## How the solution was developed

1. **Read the requirements and inspect all four images.** Bars have different widths and lengths, and some touch the borders.
2. **Find the useful guarantee.** Every allowed bar crosses the middle row.
3. **Count black stretches on that row only.** Scanning more rows would find the same bars again.
4. **Handle JPEG shading and bad input.** Use a brightness threshold, validate the path and file format, and report exceptions.
5. **Test and package.** Test the supplied images plus extra cases, then include the executable, source, build script, and tests.

## Build from source and run tests

Building turns the C# source into an executable. Skip this section if you only want to run the included program.

1. Install [Visual Studio](https://visualstudio.microsoft.com/downloads/) with the **.NET desktop development** workload.
2. Make sure the **.NET Framework 4.8 Developer Pack** (targeting pack) is installed. You can get it from [Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48). The runtime alone cannot build.
3. In PowerShell, from the project root, run:

```powershell
.\build.ps1 -Test
```

This builds both projects in Release mode and runs the test suite. All tests should pass, and the runner ends with a summary such as:

```text
33 passed; 0 failed.
```

The tests cover:

- all four supplied images
- wrong argument counts and bad file paths
- non-JPEG files and unreadable files
- bars touching the image borders
- very thin bars and very thin gaps
- JPEG compression artefacts
- paths that contain spaces
- releasing the file handle after reading
- **100 randomly generated JPEG images** with known bar counts

Run the newly built program with:

```powershell
.\src\TMMC.LineCounter\bin\Release\TMMC.LineCounter.exe "$PWD\test-images\img_4.jpg"
```

> **Building does not replace the program in `app/`.** The `app/` copy is the executable built from this source with `build.ps1`. After changing code, run the new build from `bin\Release` instead.

To build without tests, run `.\build.ps1`. You can also open `TMMC.LineCounter.sln` in Visual Studio and choose **Build > Build Solution**. The tests use a small console runner, not Visual Studio Test Explorer or `dotnet test`. No third-party NuGet packages are needed.

## Errors and troubleshooting

A successful run prints **only the count**. Errors go to standard error, which normally shows in the same window.

| Exit code | Meaning |
| --- | --- |
| `0` | Success |
| `1` | Invalid path, unreadable or non-JPEG file, or another handled exception |
| `2` | Wrong number of arguments |

Check the exit code by running `$LASTEXITCODE` straight after the program.

Example — running with no argument:

```powershell
.\app\TMMC.LineCounter.exe
$LASTEXITCODE
```

```text
Usage: TMMC.LineCounter.exe "<full path to JPEG image>"
2
```

| Problem | Fix |
| --- | --- |
| Executable not found | Open PowerShell in the project root (the folder with `app` and `test-images`). |
| "Exactly one argument required" | Give one quoted image path after the executable. |
| "Absolute path required" | Use `$PWD` as shown above, or a full path such as `C:\Images\picture.jpg`. |
| Image cannot be opened | Check the file exists, is readable, and really is a JPEG. Renaming a PNG to `.jpg` does not convert it. |
| Build cannot find framework references | Install the **4.8 Developer Pack**, not just the runtime. |
| Code changes do not appear | Run the program in `src\TMMC.LineCounter\bin\Release`, not the copy in `app`. |

If PowerShell blocks `build.ps1`, first read the script. For a copy you trust, run:

```powershell
Unblock-File -LiteralPath .\build.ps1
Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned
.\build.ps1 -Test
```

This policy change lasts only for the current PowerShell window. Settings managed by an organisation may override it; see [Microsoft's execution-policy guide](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy).

## Limitations

The method trusts the assignment's image rules and does not check every one of them.

- **Bars that miss the middle row are not counted.** For example, a short bar only in the top half.
- **Touching bars count as one,** because no white separates them.
- **Noise on the middle row can change the count.** A bright speck inside a bar would split it into two stretches; a dark speck in a white gap would add a false stretch. The brightness threshold handles normal JPEG compression, but not heavy noise or very low quality settings.
- **Only Windows is supported.**
- Normal exceptions are caught and reported, but the program cannot prevent being killed from outside or crashes of the runtime itself.

A possible improvement for noisier images would be to scan a few rows around the middle and take the majority count.

## Project files

| File or folder | Purpose |
| --- | --- |
| `app/` | Ready-to-run executable and its runtime config |
| `src/TMMC.LineCounter/Program.cs` | Argument check, console output, exit codes, exception handling |
| `src/TMMC.LineCounter/VerticalLineCounter.cs` | Path and JPEG checks, image loading, counting algorithm |
| `tests/` | Automated test runner and its project |
| `test-images/` | The four original JPEG examples |
| `TMMC.LineCounter.sln` | Visual Studio solution containing both projects |
| `build.ps1` | Builds the solution and optionally runs the tests |
| `.gitignore` | Excludes temporary build and editor files |
