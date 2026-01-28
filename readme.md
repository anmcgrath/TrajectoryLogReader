# TrajectoryLogReader

A .NET library for parsing, analyzing, and compressing Varian TrueBeam trajectory log files (*.bin).
Supports standard Version 5.0 logs.

## Installation

```bash
dotnet add package TrajectoryLogReader
```

## Basic Usage

### Reading Logs

```csharp
using TrajectoryLogReader;
using TrajectoryLogReader.IO;

// Read standard binary log
var log = LogReader.ReadBinary("path/to/file.bin");
```

## Data Model

The `TrajectoryLog` object provides two primary views of the data: **Axes** (columnar) and **Snapshots** (row-based).

### 1. Columnar Access (Axes)

Best for statistical analysis or plotting full time-series.

```csharp
// Access raw arrays (IEnumerable<float>)
var gantryAngles = log.Axes.Gantry.ActualValues;
var cumulativeMu = log.Axes.MU.ActualValues;
var gantryErrors = log.Axes.Gantry.ErrorValues;

// Derived rates
var gantrySpeed = log.Axes.GantrySpeed.ActualValues; // deg/s
var doseRate = log.Axes.DoseRate.ActualValues;   
```

### 2. Temporal Access (Snapshots)

Best for sequential processing or state reconstruction.

```csharp
foreach (var snapshot in log.Snapshots)
{
    var time = snapshot.Milliseconds;
    var gantry = snapshot.Gantry.Actual;
    
    // Access MLC leaf positions with 0-based index [bankIndex, leafIndex]
    var leafA1 = snapshot.Mlc.Leaves[0][0].Actual; 
}
```

These can also be access for each sub-beam, e.g

```csharp
log.SubBeams.First().Axes.Gantry.ExpectedValues;
log.SubBeams.First().Snapshots.First().GantryRtn.Actual;
```

## Analysis Features

### Interpolation

Linear interpolation of machine state at any given time (ms).

```csharp
int timeMs = 150;
// Axis position
double gantryAtT = log.InterpolateAxisData(Axis.GantryRtn, timeMs, RecordType.ActualPosition);
// Specific MLC leaf
double leafPos = log.InterpolateMLCPosition(timeMs, bank: 0, leafIndex: 10, RecordType.ActualPosition);
```

### Fluence Reconstruction

Generates a 2D fluence map using Sutherland-Hodgman polygon clipping for accurate aperture area calculation.

```csharp
var options = new FluenceOptions
{
    Rows = 512,
    Cols = 512,
    Width = 400, //mm
    Height = 400, // mm
};

var fluenceGrid = log.CreateFluence(options, RecordType.ActualPosition);
var fluenceGridBeam = log.SubBeams.First().CreateFluence(options, RecordType.ActualPosition);
```

<img width="400" height="400" alt="image" src="https://github.com/user-attachments/assets/738b82fc-f0a6-43d3-bbd5-4a9b6b266bb6" />

### Gamma Analysis

Implements 2D Gamma Index (Low et al., 1998). Uses interpolation of the eval grid, which is configurable. The search
radius is also configurable.

```csharp
var gammaParams = new GammaParameters2D(dtaTolMm: 1, doseTolPercent: 1, global: true);
var result = GammaCalculator2D.Calculate(gammaParams, reference, evaluated);

Console.WriteLine($"Passing Rate: {result.FracPass * 100:F1}%");
```

### Output

Output a log-file to TSV or CSV

```csharp
log.SaveToCsv("/out/log.csv", includeHeaders: true, AxisScale.IEC61217);
```

<img width="50%" alt="image" src="https://github.com/user-attachments/assets/9f72a8e7-bc61-4f14-87a8-289dc80334cb" />


### Writing Compressed Logs

The CompressedLogWriter will write the files to a compressed (***lossy***) format. Here we only store changes between
each axis position as scaled 8/16 bit values.

While there is some loss in precision, the maximum loss for each axis is:

| Axis                  | Stream Type | Scale | Notes               |
|-----------------------|-------------|-------|---------------------|
| MLC (All)             | Small       | 1000  | 0.001 cm res        |
| Jaws (X1, X2, Y1, Y2) | Small       | 1000  | 0.001 cm res        |
| Couch Pitch / Roll    | Small       | 100   | 0.01 deg res        |
| Couch Vrt / Lng / Lat | Large       | 100   | 0.01 cm res         |
| Gantry Rtn            | Large       | 100   | 0.01 deg res, Wraps |
| Collimator Rtn        | Large       | 100   | 0.01 deg res, Wraps |
| Couch Rtn             | Large       | 100   | 0.01 deg res, Wraps |
| MU                    | Large       | 1000  | 0.001 unit res      |
| ControlPoint          | Large       | 1000  | 0.001 unit res      |

Log file sizes are reduced by >90%, depending on the file.

```csharp
// Writes to .cbin
CompressedLogWriter.Write(log, "path/to/output.cbin");
```
