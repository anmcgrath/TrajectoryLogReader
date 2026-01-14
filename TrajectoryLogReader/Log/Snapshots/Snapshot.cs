using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log.Snapshots;

/// <summary>
/// Represents a single snapshot of measurement data at a specific time point.
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
    /// The index of this measurement in the log sequence.
    /// </summary>
    public int MeasIndex => _measIndex;

    /// <summary>
    /// The time in milliseconds from the start of the log.
    /// </summary>
    public int TimeInMs => _log.Header.SamplingIntervalInMS * _measIndex;

    /// <summary>
    /// Collimator rotation angle (in degrees)
    /// </summary>
    public ScalarRecord CollRtn
    {
        get
        {
            field ??= new(_log, Axis.CollRtn, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Gantry rotation angle (in degrees)
    /// </summary>
    public ScalarRecord GantryRtn
    {
        get
        {
            field ??= new(_log, Axis.GantryRtn, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Y1 jaw position (in cm)
    /// </summary>
    public ScalarRecord Y1
    {
        get
        {
            field ??= new(_log, Axis.Y1, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Y2 jaw position (in cm)
    /// </summary>
    public ScalarRecord Y2
    {
        get
        {
            field ??= new(_log, Axis.Y2, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// X1 jaw position (in cm)
    /// </summary>
    public ScalarRecord X1
    {
        get
        {
            field ??= new(_log, Axis.X1, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// X2 jaw position (in cm)
    /// </summary>
    public ScalarRecord X2
    {
        get
        {
            field ??= new(_log, Axis.X2, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch vertical position (in cm)
    /// </summary>
    public ScalarRecord CouchVrt
    {
        get
        {
            field ??= new(_log, Axis.CouchVrt, _measIndex);
            return (field);
        }
    }

    /// <summary>
    /// Couch longitudinal position (in cm)
    /// </summary>
    public ScalarRecord CouchLng
    {
        get
        {
            field ??= new(_log, Axis.CouchLng, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch lateral position (in cm)
    /// </summary>
    public ScalarRecord CouchLat
    {
        get
        {
            field ??= new(_log, Axis.CouchLat, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch rotation angle (in degrees)
    /// </summary>
    public ScalarRecord CouchRtn
    {
        get
        {
            field ??= new(_log, Axis.CouchRtn, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch pitch angle (in degrees).
    /// </summary>
    public ScalarRecord CouchPitch
    {
        get
        {
            field ??= new(_log, Axis.CouchPitch, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Couch roll angle (in degrees).
    /// </summary>
    public ScalarRecord CouchRoll
    {
        get
        {
            field ??= new(_log, Axis.CouchRoll, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Monitor units delivered.
    /// </summary>
    public ScalarRecord MU
    {
        get
        {
            field ??= new(_log, Axis.MU, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Beam hold status.
    /// </summary>
    public ScalarRecord BeamHold
    {
        get
        {
            field ??= new(_log, Axis.BeamHold, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Current control point index.
    /// </summary>
    public ScalarRecord ControlPoint
    {
        get
        {
            field ??= new(_log, Axis.ControlPoint, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Target position.
    /// </summary>
    public ScalarRecord TargetPosition
    {
        get
        {
            field ??= new(_log, Axis.TargetPosition, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Tracking target position.
    /// </summary>
    public ScalarRecord TrackingTarget
    {
        get
        {
            field ??= new(_log, Axis.TrackingTarget, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Tracking phase.
    /// </summary>
    public ScalarRecord TrackingPhase
    {
        get
        {
            field ??= new(_log, Axis.TrackingPhase, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Tracking base position.
    /// </summary>
    public ScalarRecord TrackingBase
    {
        get
        {
            field ??= new(_log, Axis.TrackingBase, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// Tracking conformity index.
    /// </summary>
    public ScalarRecord TrackingConformityIndex
    {
        get
        {
            field ??= new(_log, Axis.TrackingConformityIndex, _measIndex);
            return field;
        }
    }

    /// <summary>
    /// MLC leaf positions (in cm)
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
    /// Retrieves a scalar record for a specific axis.
    /// </summary>
    /// <param name="axis">The axis to retrieve.</param>
    /// <returns>The scalar record for the axis.</returns>
    public ScalarRecord GetScalarRecord(Axis axis)
    {
        if (axis == Axis.MLC)
            throw new Exception($"Cannot get scalar record for MLC. Use MLC to get MLC data.");

        return new ScalarRecord(_log, axis, _measIndex);
    }

    /// <summary>
    /// Returns the next snapshot. Null if this is the first
    /// </summary>
    /// <returns></returns>
    public Snapshot? Previous()
    {
        if (_measIndex == 0)
            return null;
        return new Snapshot(_measIndex - 1, _log);
    }

    /// <summary>
    /// Returns the next snapshot. Null if this is the last.
    /// </summary>
    /// <returns></returns>
    public Snapshot? Next()
    {
        if (_measIndex == _log.Header.NumberOfSnapshots - 1)
            return null;
        return new Snapshot(_measIndex + 1, _log);
    }

    /// <summary>
    /// The MLC model used.
    /// </summary>
    public IMLCModel MlcModel => _log.MlcModel;
}