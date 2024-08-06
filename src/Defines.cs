using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using System;


enum ControllerType
{
    Real,
    Virtual,
    None
}

internal class Utils
{
    public static NetworkScannerSearchCriterias ConvertControllerType(ControllerType type) 
    {
        switch (type)
        {
            case ControllerType.Real:
                return NetworkScannerSearchCriterias.Real;

            case ControllerType.Virtual:
                return NetworkScannerSearchCriterias.Virtual;

            default:
                return NetworkScannerSearchCriterias.None;
        }
    }
}

internal interface ISendable 
{
     String GetMessage();
}

struct Point
{
    public float x;
    public float y;
    public float z;

    public Point(float _x, float _y, float _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }
}

struct Rotation
{
    public float x;
    public float y;
    public float z;
    public float w;

    public Rotation(float _x, float _y, float _z, float _w)
    {
        x = _x;
        y = _y;
        z = _z;
        w = _w;
    }

    // Hardcoded for now
    public static Rotation Default => new Rotation(0.0f, 1.0f, 0.0f, 0.0f);
}

struct Target : ISendable
{
    public Point pos;
    public Rotation ort;

    public Target(Point _pos, Rotation _ort)
    {
        pos = _pos;
        ort = _ort;
    }

    public Target(Point _pos)
    {
        pos = _pos;
        ort = Rotation.Default;
    }

    public String GetMessage()
    {
        // Hardcoded for now
        return $"robtarget;[[{pos.x},{pos.y},{pos.z}],[{ort.x},{ort.y},{ort.z},{ort.w}],[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]]";
    }
}

struct SpeedData
{
    public int tcp;
    public int ori;
    public int leax;
    public int reax;

    public SpeedData(int _tcp, int _ori, int _leax, int _reax)
    {
        tcp = _tcp;
        ori = _ori;
        leax = _leax;
        reax = _reax;
    }
}

struct TargetSpeedData : ISendable
{
    public Point pos;
    public Rotation ort;
    public SpeedData speed_data;

    public TargetSpeedData(Point _pos, Rotation _ort, SpeedData _sd)
    {
        pos = _pos;
        ort = _ort;
        speed_data = _sd;
    }

    public TargetSpeedData(Point _pos, SpeedData _sd)
    {
        pos = _pos;
        ort = Rotation.Default;
        speed_data = _sd;
    }
    public String GetMessage()
    {
        // Hardcoded for now
        return $"target_speed_data;[[[{pos.x},{pos.y},{pos.z}],[{ort.x},{ort.y},{ort.z},{ort.w}],[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]],[{speed_data.tcp}, {speed_data.ori}, {speed_data.leax}, {speed_data.reax}]]";
    }
}
