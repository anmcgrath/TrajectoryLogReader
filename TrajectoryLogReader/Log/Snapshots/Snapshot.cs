using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log.Snapshots;

/// <summary>
/// Represents one sampled time point from a trajectory log. Each property exposes the
/// expected and actual value for a treatment axis at the same
/// sampling instant.
/// </summary>
public class Snapshot
{
    internal Snapshot(int measIndex, TrajectoryLog log)
    {
        _measIndex = measIndex;
        _log = log;
    }

    private readonly int _measIndex;
    private readonly TrajectoryLog _log;

    /// <summary>
    /// The zero-based snapshot index within the log.
    /// </summary>
    public int MeasIndex => _measIndex;

    /// <summary>
    /// Elapsed time from the start of the log in milliseconds, computed from the header
    /// sampling interval.
    /// </summary>
    public int TimeInMs => _log.Header.SamplingIntervalInMS * _measIndex;

    /// <summary>
    /// Collimator rotation angle in degrees. Use <see cref="IScalarRecord.WithScale"/>
    /// if you need IEC 61217 conventions regardless of the log's native scale.
    /// </summary>
    public IScalarRecord CollRtn
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.CollRtn, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Gantry rotation angle in degrees. 
    /// </summary>
    public IScalarRecord GantryRtn
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.GantryRtn, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Y1 jaw position in centimeters at isocenter. Interpret sign/direction using the
    /// record's effective scale.
    /// </summary>
    public IScalarRecord Y1
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.Y1, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Y2 jaw position in centimeters at isocenter.
    /// </summary>
    public IScalarRecord Y2
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.Y2, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// X1 jaw position in centimeters at isocenter.
    /// </summary>
    public IScalarRecord X1
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.X1, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// X2 jaw position in centimeters at isocenter.
    /// </summary>
    public IScalarRecord X2
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.X2, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch vertical position in centimeters.
    /// </summary>
    public IScalarRecord CouchVrt
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.CouchVrt, _measIndex);
            return (field);
        }
    }

    /// <summary>
    /// Couch longitudinal position in centimeters.
    /// </summary>
    public IScalarRecord CouchLng
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.CouchLng, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch lateral position in centimeters.
    /// </summary>
    public IScalarRecord CouchLat
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.CouchLat, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch rotation angle (yaw) in degrees.
    /// </summary>
    public IScalarRecord CouchRtn
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.CouchRtn, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch pitch angle in degrees.
    /// </summary>
    public IScalarRecord CouchPitch
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.CouchPitch, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch roll angle in degrees.
    /// </summary>
    public IScalarRecord CouchRoll
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.CouchRoll, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Cumulative monitor units (MU). For dose-weighted analyses, prefer <see cref="DeltaMu"/>.
    /// </summary>
    public IScalarRecord MU
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.MU, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Beam hold status
    /// </summary>
    public IScalarRecord BeamHold
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.BeamHold, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Current control point index as reported in the log. This is useful when mapping
    /// delivery back to plan control points.
    /// </summary>
    public IScalarRecord ControlPoint
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.ControlPoint, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Target position for tracking-enabled deliveries.
    /// </summary>
    public IScalarRecord TargetPosition
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.TargetPosition, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Tracking target position used by the motion management subsystem.
    /// </summary>
    public IScalarRecord TrackingTarget
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.TrackingTarget, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Tracking phase indicator
    /// </summary>
    public IScalarRecord TrackingPhase
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.TrackingPhase, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Tracking base position
    /// </summary>
    public IScalarRecord TrackingBase
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.TrackingBase, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Tracking conformity index, when available.
    /// </summary>
    public IScalarRecord TrackingConformityIndex
    {
        get
        {
            field ??= new ScalarRecord(_log, Axis.TrackingConformityIndex, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Incremental MU delivered since the previous snapshot. This is the most appropriate
    /// weight for dose-proportional metrics.
    /// </summary>
    public IScalarRecord DeltaMu
    {
        get
        {
            field ??= new DeltaRecord(_log, Axis.MU, _measIndex, 1f);
            return field;
        }
    }

    /// <summary>
    /// The speed of the gantry, in degrees/second.
    /// </summary>
    public IScalarRecord GantrySpeed
    {
        get
        {
            field ??= GetDeltaRecord(Axis.GantryRtn, TimeSpan.FromSeconds(1));
            return field;
        }
    }

    /// <summary>
    /// The dose rate, in MU per minute
    /// </summary>
    public IScalarRecord DoseRate
    {
        get
        {
            field ??= GetDeltaRecord(Axis.GantryRtn, TimeSpan.FromMinutes(1));
            return field;
        }
    }

    /// <summary>
    /// MLC leaf positions for this snapshot. Leaf positions are exposed in both banks and
    /// follow the log's axis scale unless converted explicitly.
    /// </summary>
    public MLCSnapshot MLC
    {
        get
        {
            field ??= new(_log, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Retrieves a scalar axis record by enum value. This is helpful for generic analyses
    /// (for example iterating over sampled axes) without hard-coding property names.
    /// </summary>
    /// <param name="axis">The scalar axis to retrieve.</param>
    /// <returns>A scalar record for the requested axis.</returns>
    /// <exception cref="Exception">Thrown when requesting <see cref="Axis.MLC"/>.</exception>
    public IScalarRecord GetScalarRecord(Axis axis)
    {
        if (axis == Axis.MLC)
            throw new Exception($"Cannot get scalar record for MLC. Use MLC to get MLC data.");

        return new ScalarRecord(_log, axis, _measIndex);
    }

    /// <summary>
    /// Retrieves a delta axis, e.g DeltaMu or DeltaGantry
    /// </summary>
    /// <param name="axis">The scalar axis to retrieve.</param>
    /// <param name="timeSpan">Specify a timespan to get a speed, e.g for doserate (MU/min) specify TimeSpan.FromMinutes(1)</param>
    /// <returns>A delta record for the requested axis, with a speed if </returns>
    /// <exception cref="Exception">Thrown when requesting <see cref="Axis.MLC"/>.</exception>
    public IScalarRecord GetDeltaRecord(Axis axis, TimeSpan? timeSpan)
    {
        if (axis == Axis.MLC)
            throw new Exception($"Cannot get scalar record for MLC. Use MLC to get MLC data.");

        var msConverter = !timeSpan.HasValue ? 1 : timeSpan.Value.TotalMilliseconds / _log.Header.SamplingIntervalInMS;
        return new DeltaRecord(_log, axis, _measIndex, (float)msConverter);
    }

    /// <summary>
    /// Returns the previous snapshot in time, or <see langword="null"/> if this is the first.
    /// </summary>
    /// <returns>The prior snapshot, if one exists.</returns>
    public Snapshot? Previous()
    {
        if (_measIndex == 0)
            return null;
        return new Snapshot(_measIndex - 1, _log);
    }

    /// <summary>
    /// Returns the next snapshot in time, or <see langword="null"/> if this is the last.
    /// </summary>
    /// <returns>The subsequent snapshot, if one exists.</returns>
    public Snapshot? Next()
    {
        if (_measIndex == _log.Header.NumberOfSnapshots - 1)
            return null;
        return new Snapshot(_measIndex + 1, _log);
    }

    /// <summary>
    /// The MLC model associated with this log. Use this to interpret leaf indexing,
    /// leaf widths, and bank conventions correctly.
    /// </summary>
    public IMLCModel MlcModel => _log.MlcModel;
}