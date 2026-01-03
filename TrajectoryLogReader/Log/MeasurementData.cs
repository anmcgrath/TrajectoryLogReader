namespace TrajectoryLogReader.Log;

public class MeasurementData
{
    internal MeasurementData(int measIndex, TrajectoryLog log)
    {
        _measIndex = measIndex;
        _log = log;
    }

    private readonly int _measIndex;
    private readonly TrajectoryLog _log;

    public int MeasIndex => _measIndex;

    public int TimeInMs => _log.Header.SamplingIntervalInMS * _measIndex;

    private ScalarRecord _colRtn;

    public ScalarRecord CollRtn
    {
        get
        {
            _colRtn ??= new(_log, Axis.CollRtn, _measIndex);
            return _colRtn;
        }
    }

    private ScalarRecord _gantryRtn;

    public ScalarRecord GantryRtn
    {
        get
        {
            _gantryRtn ??= new(_log, Axis.GantryRtn, _measIndex);
            return _gantryRtn;
        }
    }

    private ScalarRecord _y1;

    public ScalarRecord Y1
    {
        get
        {
            _y1 ??= new(_log, Axis.Y1, _measIndex);
            return _y1;
        }
    }

    private ScalarRecord _y2;

    public ScalarRecord Y2
    {
        get
        {
            _y2 ??= new(_log, Axis.Y2, _measIndex);
            return _y2;
        }
    }

    private ScalarRecord _x1;

    public ScalarRecord X1
    {
        get
        {
            _x1 ??= new(_log, Axis.X1, _measIndex);
            return _x1;
        }
    }

    private ScalarRecord _x2;

    public ScalarRecord X2
    {
        get
        {
            _x2 ??= new(_log, Axis.X2, _measIndex);
            return _x2;
        }
    }

    private ScalarRecord _couchVrt;

    public ScalarRecord CouchVrt
    {
        get
        {
            _couchVrt ??= new(_log, Axis.CouchVrt, _measIndex);
            return (_couchVrt);
        }
    }

    private ScalarRecord _couchLng;

    public ScalarRecord CouchLng
    {
        get
        {
            _couchLng ??= new(_log, Axis.CouchLng, _measIndex);
            return _couchLng;
        }
    }

    private ScalarRecord _couchLat;

    public ScalarRecord CouchLat
    {
        get
        {
            _couchLat ??= new(_log, Axis.CouchLat, _measIndex);
            return _couchLat;
        }
    }

    private ScalarRecord _couchRtn;

    public ScalarRecord CouchRtn
    {
        get
        {
            _couchRtn ??= new(_log, Axis.CouchRtn, _measIndex);
            return _couchRtn;
        }
    }

    private ScalarRecord _couchPitch;

    public ScalarRecord CouchPitch
    {
        get
        {
            _couchPitch ??= new(_log, Axis.CouchPitch, _measIndex);
            return _couchPitch;
        }
    }

    private ScalarRecord _couchRoll;

    public ScalarRecord CouchRoll
    {
        get
        {
            _couchRoll ??= new(_log, Axis.CouchRoll, _measIndex);
            return _couchRoll;
        }
    }

    private ScalarRecord _mu;

    public ScalarRecord MU
    {
        get
        {
            _mu ??= new(_log, Axis.MU, _measIndex);
            return _mu;
        }
    }

    private ScalarRecord _beamHold;

    public ScalarRecord BeamHold
    {
        get
        {
            _beamHold ??= new(_log, Axis.BeamHold, _measIndex);
            return _beamHold;
        }
    }

    private ScalarRecord _controlPoint;

    public ScalarRecord ControlPoint
    {
        get
        {
            _controlPoint ??= new(_log, Axis.ControlPoint, _measIndex);
            return _controlPoint;
        }
    }

    private ScalarRecord _targetPosition;

    public ScalarRecord TargetPosition
    {
        get
        {
            _targetPosition ??= new(_log, Axis.TargetPosition, _measIndex);
            return _targetPosition;
        }
    }

    private ScalarRecord _trackingTarget;

    public ScalarRecord TrackingTarget
    {
        get
        {
            _trackingTarget ??= new(_log, Axis.TrackingTarget, _measIndex);
            return _trackingTarget;
        }
    }

    private ScalarRecord _trackingPhase;

    public ScalarRecord TrackingPhase
    {
        get
        {
            _trackingPhase ??= new(_log, Axis.TrackingPhase, _measIndex);
            return _trackingPhase;
        }
    }

    private ScalarRecord _trackingBase;

    public ScalarRecord TrackingBase
    {
        get
        {
            _trackingBase ??= new(_log, Axis.TrackingBase, _measIndex);
            return _trackingBase;
        }
    }

    private ScalarRecord _trackingConformityIndex;

    public ScalarRecord TrackingConformityIndex
    {
        get
        {
            _trackingConformityIndex ??= new(_log, Axis.TrackingConformityIndex, _measIndex);
            return _trackingConformityIndex;
        }
    }

    private MLCRecord _mlc;

    public MLCRecord MLC
    {
        get
        {
            _mlc ??= new(_log, _measIndex);
            return _mlc;
        }
    }

    public ScalarRecord GetScalarRecord(Axis axis)
    {
        return new ScalarRecord(_log, axis, _measIndex);
    }
}