# Trajectory Log Reader

Reads Varian TrueBeam trajectory log files (*.bin). **This has only been tested with log-files of version 5.0.**

## Usage

Install from NuGet

```commandline
dotnet add package TrajectoryLogReader
```

Load the log file:

```csharp
TrajectoryLog log = LogReader.ReadBinary(filePath);
```

## Data access

Log-file data can be accessed through either a "column-based" (axes) or "row-based" (snapshots) approach.

### Axes data

```log.Axes``` provides a wrapper around the raw-data for each axis.

```csharp
var expected = log.Axes.Gantry.ExpectedValues;
var actual = log.Axes.Gantry.ActualValues;
var errors = log.Axes.Gantry.ErrorValues;
var expectedSubBeam1 = log.SubBeams.First().Axes.Gantry.ExpectedValues;
```

### Snapshots

Reading all snapshots can be done via:

```csharp
foreach (var snapshot in log.Snapshots)
{
    Console.WriteLine(snapshot.X1.Actual);
    Console.WriteLine(snapshot.X1.Expected);
}
```

The same can be performed for sub-beams:

```csharp
foreach (var sub in log.SubBeams)
{
    foreach (var snapshot in sub.Snapshots)
    {
        Console.WriteLine(snapshot.X1.Actual);
        Console.WriteLine(snapshot.X1.Expected);
    }
}
```

### Interpolation

There are some useful functions for interpolating data at various times. For example:

```csharp
int timeInMs = 20;
var gantryExpected = log.InterpolateAxisData(Axis.Gantry, timeInMs, RecordType.ExpectedPosition);
var gantryActual = log.InterpolateAxisData(Axis.Gantry, timeInMs, RecordType.ActualPosition);
```

Data is linearly interpolated between sampling intervals.

MLC leaf positions can be interpolated at time t:

```csharp
int bankA = 0;
int bankB = 1;
int leafIndex = 0;
int timeInMs = 25;
double mlc = log.InterpolateMLCPosition(timeInMs, bankA, leafIndex, RecordType.Actual);
```

Or, a 2D array of MLC positions interpolated at time t can be extracted:

```csharp
int t = 25; // time in ms
float[,] mlc = log.InterpolateMLCPositions(t, RecordType.Expected);
```

## Fluence Reconstruction

The library reconstructs the delivered 2D fluence by temporally integrating the beam intensity over the course of the
delivery. This provides a high-fidelity representation of the actual modulation delivered by the linac.

```csharp
var fluence = log.CreateFluence(options, RecordType.Actual); // for the entire log file
var fluenceSubBeam = log.SubBeams.First().CreateFluence(options, RecordType.Actual); // just for this beam
```

**Dosimetric Principles:**

* **Aperture Integration**: The total fluence is the summation of instantaneous apertures defined by the MLC and Jaws at
  each control point, weighted by the incremental MU delivered.
* **Geometric Accuracy**: The algorithm accounts for dynamic collimator rotation and asymmetric jaw tracking.
* **Sub-pixel Resolution**: To accurately model high-modulation VMAT arcs and small stereotactic fields, the system uses
  an exact area-intersection method (Sutherland-Hodgman) rather than simple center-point sampling. This eliminates
  aliasing artifacts and partial-volume errors at the leaf edges.

## Gamma Analysis

The verification module implements the standard 2D Gamma Index analysis (Low et al., 1998) to quantify the agreement
between the Reference (Plan) and Evaluated (Log) distributions.

**Algorithm Specifics:**

* **Grid Resampling**: To minimize discretization error in high-gradient regions (penumbra), the Reference distribution
  is automatically upsampled. The resolution is set to ensure it is significantly smaller than the
  Distance-to-Agreement (DTA) tolerance (default $\le \frac{1}{5} \text{DTA}$).
* **Composite Metric**: The algorithm evaluates the generalized $\gamma$ function, combining dose
  difference ($\Delta D$) and spatial distance ($\Delta d$) criteria. It supports both **Global Normalization** (
  relative to $D_{max}$) and **Local Normalization**.
* **Efficient Search**: A localized search window (default radius $2 \times \text{DTA}$) is used for each evaluated point to
  find the minimum $\gamma$ value.


