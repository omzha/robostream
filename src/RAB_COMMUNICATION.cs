using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
//using ABB.Robotics.Controllers.RapidDomain;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

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

class RAB_COM
{
    // Controller
    public Controller Controller;

    // Queues
    private IpcQueue Robot_Queue;
    private IpcQueue SDK_Queue;

    // Messages
    private IpcMessage PositionMessage;
    private IpcMessage ExitMessage;
    private IpcMessage ReturnMessage;

    private int MessageSize;
    private bool MovingToTarget = false;
    private int ReturnMessageCount = 0;

    // Target sent events
    public event EventHandler TargetSent;

    protected virtual void OnTargetSent(EventArgs e)
    {
        TargetSent?.Invoke(this, e);
    }

    // Target reached event
    public event EventHandler TargetReached;

    protected virtual void OnTargetReached(EventArgs e)
    {
        TargetReached?.Invoke(this, e);
    }

    public RAB_COM(ControllerType type)
    {
        // Set up controller
        this.Controller = GetController(type);

        // Set up robot queue
        this.Robot_Queue = this.Controller.Ipc.GetQueue("RMQ_T_ROB1");
        this.MessageSize = this.Controller.Ipc.GetMaximumMessageSize();

        // Set up SDK queue
        if (!this.Controller.Ipc.Exists("RMQ_SDK"))
        {
            this.SDK_Queue = this.Controller.Ipc.CreateQueue("RMQ_SDK", 10, this.MessageSize);
        }
        else
        {
            // If the queue already exists, delete it and recreate it
            this.Controller.Ipc.DeleteQueue(this.Controller.Ipc.GetQueueId("RMQ_SDK"));
            this.SDK_Queue = this.Controller.Ipc.CreateQueue("RMQ_SDK", 10, this.MessageSize);
        }


#if DEBUG
        // Debug
        Console.WriteLine($"SDK Queue {this.SDK_Queue.Name}:{this.SDK_Queue.QueueId}:{this.SDK_Queue.Capacity}:{this.SDK_Queue.MessageSizeLimit}");
#endif
    }
   
    private static Controller GetController(ControllerType type)
    {
        NetworkScanner scanner = new NetworkScanner();
        //Assume single controller
        var controller_info = scanner.GetControllers(Utils.ConvertControllerType(type))[0];
        var controller_id = controller_info.SystemId;
        Controller cntr = Controller.Connect(controller_id, ConnectionType.Standalone);
        Console.WriteLine("I found a controller named: {0}", controller_info.SystemName);
        return cntr;
    }

    public void SendExitMessage()
    {
        this.ExitMessage = new IpcMessage();

        var exit = "bool;FALSE\0";

        //Create message data
        Byte[] data = new UTF8Encoding().GetBytes(exit);

        // Debug
#if DEBUG
        Console.WriteLine(exit);
        Console.WriteLine($"Data Size: {data.GetLength(0)}");
#endif

        //Place data and sender information in message
        this.ExitMessage.SetData(data);
        this.ExitMessage.Sender = SDK_Queue.QueueId;
        //Send message to the RAPID queue
        Robot_Queue.Send(this.ExitMessage);
    }

    private void SendMessageImpl(ref Byte[] bytes)
    {
        // Debug
#if DEBUG
        Console.WriteLine($"Data length: {bytes.GetLength(0)}");
#endif

        // Set message data
        this.PositionMessage.SetData(bytes);

        // Set message sender
        this.PositionMessage.Sender = SDK_Queue.QueueId;

        // Send message to the RAPID queue
        Robot_Queue.Send(this.PositionMessage);
    }

    private void PadMessage(ref String message)
    {
        int PaddingSize = MessageSize - message.Length;
        message = message.PadRight(PaddingSize, ' ');
    }

    public void SendMessage(ISendable target)
    {
        this.PositionMessage = new IpcMessage();

        String string_to_send = target.GetMessage();
        PadMessage(ref string_to_send);

#if DEBUG
        Console.WriteLine(string_to_send);      
#endif

        // Convert to byte representation
        Byte[] data = new UTF8Encoding().GetBytes(string_to_send);
        SendMessageImpl(ref data);

        OnTargetSent(EventArgs.Empty);
        MovingToTarget = true;
    }

    public void CheckReturnMsg()
    {
        this.ReturnMessage = new IpcMessage();

        IpcReturnType ret = IpcReturnType.Timeout;
        string answer = string.Empty;
        int timeout = 5000;
        //Check for msg in the PC SDK queue
        do
        {
            Console.WriteLine("Trying to get message");
            ret = SDK_Queue.Receive(timeout, this.ReturnMessage);
        } while (ret != IpcReturnType.OK);

        var string_length = this.ReturnMessage.UserDef+9;
        Console.WriteLine(string_length);
        answer = new UTF8Encoding().GetString(this.ReturnMessage.Data, 0, string_length);
        answer = answer.Replace("string;", "");

        if(++ReturnMessageCount > 1)
        {
            OnTargetReached(EventArgs.Empty);
            ReturnMessageCount = 0;
        }

        Console.WriteLine(answer);
    }
}
