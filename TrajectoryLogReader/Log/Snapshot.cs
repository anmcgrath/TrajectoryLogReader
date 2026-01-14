using TrajectoryLogReader.MLC;

namespace TrajectoryLogReader.Log;

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

    private ScalarRecord _colRtn;

    /// <summary>
    /// Collimator rotation angle (in degrees)
    /// </summary>
    public ScalarRecord CollRtn
    {
        get
        {
            _colRtn ??= new(_log, Axis.CollRtn, _measIndex);
            return _colRtn;
        }
    }

    private ScalarRecord _gantryRtn;

    /// <summary>
    /// Gantry rotation angle (in degrees)
    /// </summary>
    public ScalarRecord GantryRtn
    {
        get
        {
            _gantryRtn ??= new(_log, Axis.GantryRtn, _measIndex);
            return _gantryRtn;
        }
    }

    private ScalarRecord _y1;

    /// <summary>
    /// Y1 jaw position (in cm)
    /// </summary>
    public ScalarRecord Y1
    {
        get
        {
            _y1 ??= new(_log, Axis.Y1, _measIndex);
            return _y1;
        }
    }

    private ScalarRecord _y2;

    /// <summary>
    /// Y2 jaw position (in cm)
    /// </summary>
    public ScalarRecord Y2
    {
        get
        {
            _y2 ??= new(_log, Axis.Y2, _measIndex);
            return _y2;
        }
    }

    private ScalarRecord _x1;

    /// <summary>
    /// X1 jaw position (in cm)
    /// </summary>
    public ScalarRecord X1
    {
        get
        {
            _x1 ??= new(_log, Axis.X1, _measIndex);
            return _x1;
        }
    }

    private ScalarRecord _x2;

    /// <summary>
    /// X2 jaw position (in cm)
    /// </summary>
    public ScalarRecord X2
    {
        get
        {
            _x2 ??= new(_log, Axis.X2, _measIndex);
            return _x2;
        }
    }

    private ScalarRecord _couchVrt;

    /// <summary>
    /// Couch vertical position (in cm)
    /// </summary>
    public ScalarRecord CouchVrt
    {
        get
        {
            _couchVrt ??= new(_log, Axis.CouchVrt, _measIndex);
            return (_couchVrt);
        }
    }

    private ScalarRecord _couchLng;

    /// <summary>
    /// Couch longitudinal position (in cm)
    /// </summary>
    public ScalarRecord CouchLng
    {
        get
        {
            _couchLng ??= new(_log, Axis.CouchLng, _measIndex);
            return _couchLng;
        }
    }

    private ScalarRecord _couchLat;

    /// <summary>
    /// Couch lateral position (in cm)
    /// </summary>
    public ScalarRecord CouchLat
    {
        get
        {
            _couchLat ??= new(_log, Axis.CouchLat, _measIndex);
            return _couchLat;
        }
    }

    private ScalarRecord _couchRtn;

    /// <summary>
    /// Couch rotation angle (in degrees)
    /// </summary>
    public ScalarRecord CouchRtn
    {
        get
        {
            _couchRtn ??= new(_log, Axis.CouchRtn, _measIndex);
            return _couchRtn;
        }
    }

    private ScalarRecord _couchPitch;

    /// <summary>
    /// Couch pitch angle (in degrees).
    /// </summary>
    public ScalarRecord CouchPitch
    {
        get
        {
            _couchPitch ??= new(_log, Axis.CouchPitch, _measIndex);
            return _couchPitch;
        }
    }

    private ScalarRecord _couchRoll;

    /// <summary>
    /// Couch roll angle (in degrees).
    /// </summary>
    public ScalarRecord CouchRoll
    {
        get
        {
            _couchRoll ??= new(_log, Axis.CouchRoll, _measIndex);
            return _couchRoll;
        }
    }

    private ScalarRecord _mu;

    /// <summary>
    /// Monitor units delivered.
    /// </summary>
    public ScalarRecord MU
    {
        get
        {
            _mu ??= new(_log, Axis.MU, _measIndex);
            return _mu;
        }
    }

    private ScalarRecord _beamHold;

    /// <summary>
    /// Beam hold status.
    /// </summary>
    public ScalarRecord BeamHold
    {
        get
        {
            _beamHold ??= new(_log, Axis.BeamHold, _measIndex);
            return _beamHold;
        }
    }

    private ScalarRecord _controlPoint;

    /// <summary>
    /// Current control point index.
    /// </summary>
    public ScalarRecord ControlPoint
    {
        get
        {
            _controlPoint ??= new(_log, Axis.ControlPoint, _measIndex);
            return _controlPoint;
        }
    }

    private ScalarRecord _targetPosition;

    /// <summary>
    /// Target position.
    /// </summary>
    public ScalarRecord TargetPosition
    {
        get
        {
            _targetPosition ??= new(_log, Axis.TargetPosition, _measIndex);
            return _targetPosition;
        }
    }

    private ScalarRecord _trackingTarget;

    /// <summary>
    /// Tracking target position.
    /// </summary>
    public ScalarRecord TrackingTarget
    {
        get
        {
            _trackingTarget ??= new(_log, Axis.TrackingTarget, _measIndex);
            return _trackingTarget;
        }
    }

    private ScalarRecord _trackingPhase;

    /// <summary>
    /// Tracking phase.
    /// </summary>
    public ScalarRecord TrackingPhase
    {
        get
        {
            _trackingPhase ??= new(_log, Axis.TrackingPhase, _measIndex);
            return _trackingPhase;
        }
    }

    private ScalarRecord _trackingBase;

    /// <summary>
    /// Tracking base position.
    /// </summary>
    public ScalarRecord TrackingBase
    {
        get
        {
            _trackingBase ??= new(_log, Axis.TrackingBase, _measIndex);
            return _trackingBase;
        }
    }

    private ScalarRecord _trackingConformityIndex;

    /// <summary>
    /// Tracking conformity index.
    /// </summary>
    public ScalarRecord TrackingConformityIndex
    {
        get
        {
            _trackingConformityIndex ??= new(_log, Axis.TrackingConformityIndex, _measIndex);
            return _trackingConformityIndex;
        }
    }

    private MLCRecord _mlc;

    /// <summary>
    /// MLC leaf positions (in cm)
    /// </summary>
    public MLCRecord MLC
    {
        get
        {
            _mlc ??= new(_log, _measIndex);
            return _mlc;
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