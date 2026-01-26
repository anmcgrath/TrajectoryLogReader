# Compressed Trajectory Log Format Specification

**Version:** 1.0  
**Signature:** "VOSTLC"  
**Extension:** `.cbin` (suggested)

## Overview

The VOS Compressed Trajectory Log format is a *lossless* binary format designed to store Varian Trajectory Logs efficiently. It
utilizes delta encoding and quantization to reduce file size significantly (typically >90%),
while maintaining clinical accuracy.

The format supports an optional GZip wrapper for further compression.

## File Structure

The file consists of the following sections in order:

1. **Signature & Version**
2. **Header** (Metadata about the scan)
3. **Metadata** (Patient/Plan info block)
4. **SubBeams** (Beam definitions)
5. **Compressed Axis Data** (The actual log samples)

### 0. GZip Wrapper (Optional)

The reader automatically detects if the file is GZip compressed by checking for the magic bytes `0x1F 0x8B`. If found,
the stream is decompressed transparently before parsing the structure below.

### 1. File Identification

Same as trajectory log format, but with VOSTC as identifier.

### 2. Header - SubBeam info

Matches the standard Varian Trajectory Log structure for compatibility.

### 3. Compressed Axis Data

The data is organized by **Axis**, then by **Sample Index**, then by **Snapshot**.
This means we write the entire timeline for "Gantry" before moving to "Collimator".

**Iteration Order:**

1. For each `Axis` in `AxesSampled`:
    2. For each `Sample` in `SamplesPerAxis`:
        3. Write/Read the **Stream** of values for this specific component (size = `N_Snaps`).

#### Stream Types

Data is quantized (converted to integer) and delta-encoded. There are two stream types based on the data range required.

**Quantization Scales:**

* **Small Position (MLC, Jaws):** `1000.0` (0.001 cm resolution)
* **Large Position (Couch X/Y/Z):** `100.0` (0.01 cm resolution)
* **Angles (Gantry, Coll, Pitch, etc.):** `100.0` (0.01 degree resolution)
* **MU / ControlPoint:** `1000.0` (0.001 unit resolution)

---

#### A. Small Value Stream

**Used for:** MLC Leaves, Jaws, Couch Pitch/Roll.
**Base Storage:** 16-bit integer.
**Delta Storage:** 8-bit signed integer.

**Format:**

1. **First Value:** `int16` (Quantized Absolute Value)
2. **Next Values** (`N_Snaps - 1` times):
    * Calculate `Delta = CurrentQuantized - PreviousQuantized`.
    * **If** `Delta` is within `[-127, 127]`:
        * Write `int8` (byte) = `Delta`.
    * **Else (Escape):**
        * Write `int8` = `-128` (`0x80`).
        * Write `int16` = `CurrentQuantized` (Absolute Value).

---

#### B. Large Value Stream

**Used for:** Couch Vrt/Lng/Lat, MU, ControlPoint, Gantry Rtn, Collimator Rtn, Couch Rtn.
**Base Storage:** 32-bit integer.
**Delta Storage:** 16-bit signed integer.

**Format:**

1. **First Value:** `int32` (Quantized Absolute Value)
2. **Next Values** (`N_Snaps - 1` times):
    * Calculate `Delta = CurrentQuantized - PreviousQuantized`.
    * **Angular Handling:** If the axis is Gantry, Collimator, or Couch Rtn, the delta is normalized to the shortest
      path (e.g., 359° -> 1° becomes +2°, not -358°).
    * **If** `Delta` is within `[-32767, 32767]`:
        * Write `int16` = `Delta`.
    * **Else (Escape):**
        * Write `int16` = `-32768` (`0x8000`).
        * Write `int32` = `CurrentQuantized` (Absolute Value).

## Axis Data Classification

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
