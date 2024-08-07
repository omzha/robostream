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

struct Joint
{
    public float a1;
    public float a2;
    public float a3;
    public float a4;
    public float a5;
    public float a6;

    public Joint(float _a1, float _a2, float _a3, float _a4, float _a5, float _a6)
    {
        a1 = _a1;
        a2 = _a2;
        a3 = _a3;
        a4 = _a4;
        a5 = _a5;
        a6 = _a6;
    }

}

struct JointTarget : ISendable
{
    public Joint joint;

    public JointTarget(Joint _joint)
    {
        joint = _joint;
    }

    public String GetMessage()
    {
        return $"jointtarget;[[{joint.a1},{joint.a2},{joint.a3},{joint.a4},{joint.a5},{joint.a6}],[9E9,9E9,9E9,9E9,9E9,9E9]]";
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

public class TargetEventArgs : EventArgs
{
    public String Target {get; set;}
}
