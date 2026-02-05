# Compressed Trajectory Log Format Specification

**Version:** 2.0
**Signature:** "VOSTLC"
**Extension:** `.cbin` (suggested)

## Overview

The VOS Compressed Trajectory Log format is a *lossless* binary format designed to store Varian Trajectory Logs efficiently. It
utilizes delta encoding and dynamic per-stream quantization to reduce file size significantly (typically >90%),
while maximizing clinical accuracy.

**Version 2.0** introduces dynamic scale calculation - scales are calculated per-stream based on actual data patterns,
maximizing precision by using the full delta range available.

The format supports an optional GZip wrapper for further compression.

## File Structure

The file consists of the following sections in order:

1. **Signature & Version**
2. **Header** (Metadata about the scan)
3. **Metadata** (Patient/Plan info block)
4. **SubBeams** (Beam definitions)
5. **Per-Stream Scale Table** (NEW in v2.0)
6. **Compressed Axis Data** (The actual log samples)

### 0. GZip Wrapper (Optional)

The reader automatically detects if the file is GZip compressed by checking for the magic bytes `0x1F 0x8B`. If found,
the stream is decompressed transparently before parsing the structure below.

### 1. File Identification

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0 | 16 | string | Signature: "VOSTLC" (null-padded) |
| 16 | 16 | string | Format Version: "2.0" (null-padded) |

### 2. Header - SubBeam info

Matches the standard Varian Trajectory Log structure for compatibility.

### 3. Per-Stream Scale Table (NEW in v2.0)

The scale table stores one float per data stream (axis × samples_per_snapshot).

| Field | Type | Description |
|-------|------|-------------|
| ScaleCount | int32 | Number of scales (total streams) |
| Scales[] | float32[] | Scale factor for each stream |

**Scale Calculation:**

For each stream, the optimal scale is calculated by:
1. Collecting all delta values in the stream
2. Computing mean and standard deviation
3. Identifying outliers (> 5 standard deviations from mean)
4. Finding the maximum "normal" delta (excluding outliers)
5. Calculating scale = (delta_range × 0.9) / max_normal_delta
   - delta_range is 127 for small streams, 32767 for large streams
   - 10% headroom prevents quantization overflow

This approach optimizes precision for typical movement patterns while using escape codes for rare large deltas.

**Typical Overhead:** ~130 axes × 1 sample × 4 bytes = 520 bytes (negligible vs MB file sizes)

### 4. Compressed Axis Data

The data is organized by **Axis**, then by **Sample Index**, then by **Snapshot**.
This means we write the entire timeline for "Gantry" before moving to "Collimator".

**Iteration Order:**

1. For each `Axis` in `AxesSampled`:
    2. For each `Sample` in `SamplesPerAxis`:
        3. Write/Read the **Stream** of values for this specific component (size = `N_Snaps`).

#### Stream Types

Data is quantized (converted to integer) using the per-stream scale from the scale table, then delta-encoded.
There are two stream types based on the data range required.

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

| Axis                  | Stream Type | Notes                        |
|-----------------------|-------------|------------------------------|
| MLC (All)             | Small       | Dynamic scale per leaf       |
| Jaws (X1, X2, Y1, Y2) | Small       | Dynamic scale                |
| Couch Pitch / Roll    | Small       | Dynamic scale                |
| Couch Vrt / Lng / Lat | Large       | Dynamic scale                |
| Gantry Rtn            | Large       | Dynamic scale, Wraps at 360° |
| Collimator Rtn        | Large       | Dynamic scale, Wraps at 360° |
| Couch Rtn             | Large       | Dynamic scale, Wraps at 360° |
| MU                    | Large       | Dynamic scale                |
| ControlPoint          | Large       | Dynamic scale                |

## Dynamic Scale Benefits

**Fixed Scale (v1.0):**
- MLC uses scale 1000 → 0.01mm precision
- If max delta is 0.05cm, uses 50 of ±127 range → wasted precision

**Dynamic Scale (v2.0):**
- Scan data to find actual max delta per stream
- Calculate scale = delta_range / max_delta
- Example: max_delta 0.05cm → scale 2540 → 0.004mm precision

Outlier detection ensures that rare large movements don't degrade precision for the entire stream.
These outliers are handled via the existing escape code mechanism.
