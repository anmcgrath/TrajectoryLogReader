# Trajectory Log Reader

Reads Varian TrueBeam trajectory log files (*.bin). **This has only been tested with log-files of version 5.0.**

## Usage

```csharp
TrajectoryLog log = TrajectoryLogReader.ReadBinary(filePath);
```

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