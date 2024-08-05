using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
//using ABB.Robotics.Controllers.RapidDomain;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

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

    public static Rotation Default => new Rotation(0.0f, 1.0f, 0.0f, 0.0f);
}

struct Target
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

    public override String ToString()
    {
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

struct TargetSpeedData
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
    public override String ToString()
    {
        return $"target_speed_data;[[[{pos.x},{pos.y},{pos.z}],[{ort.x},{ort.y},{ort.z},{ort.w}],[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]],[{speed_data.tcp}, {speed_data.ori}, {speed_data.leax}, {speed_data.reax}]]";
    }
}

class RAB_COM
{
    public Controller controller;
    public IpcQueue ROB_Queue;

    public IpcQueue TheOneAndOnlyQueue;

    public IpcMessage sendPositionMessage;

    public IpcMessage sendExitMessage;

    public IpcMessage returnMessage;

    public int MessageSize;
   
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [MTAThread]
    static void Main(string[] args)
    {
        RAB_COM comm = new RAB_COM();

        //Get controller
        comm.controller = GetController(NetworkScannerSearchCriterias.Virtual);
        //comm.controller = GetController(NetworkScannerSearchCriterias.Real);

        //get T_ROB1 queue to send msgs to RAPID task
        comm.ROB_Queue = comm.controller.Ipc.GetQueue("RMQ_T_ROB1");
        //create my own PC SDK queue to receive msgs

        comm.MessageSize = Ipc.MaxMessageSize;

        if (!comm.controller.Ipc.Exists("ONLY_Q"))
        {
            comm.TheOneAndOnlyQueue = comm.controller.Ipc.CreateQueue("ONLY_Q", 10, comm.MessageSize);
        }
        else
        {
            comm.controller.Ipc.DeleteQueue(comm.controller.Ipc.GetQueueId("ONLY_Q"));
            comm.TheOneAndOnlyQueue = comm.controller.Ipc.CreateQueue("ONLY_Q", 10, comm.MessageSize);
            //comm.TheOneAndOnlyQueue = comm.controller.Ipc.GetQueue("ONLY_Q");
        }

        var queue_name = comm.TheOneAndOnlyQueue.Name;
        var queue_id = comm.TheOneAndOnlyQueue.QueueId;
        var capacity = comm.TheOneAndOnlyQueue.Capacity;
        var messagesize = comm.TheOneAndOnlyQueue.MessageSizeLimit;

        Console.WriteLine($"Queue {queue_name}:{queue_id}:{capacity}:{messagesize}");

        //Create IpcMessage objects for sending and receiving
        comm.sendExitMessage = new IpcMessage();

        ////Create a return message
        //comm.sendPositionMessage = new IpcMessage();

        ////Create a return message
        //comm.returnMessage = new IpcMessage();

        ////in an event handler, eg. button_Click
        //comm.SendMessage(new Point(1809, -166, 1248));
        //comm.CheckReturnMsg(); //Target Acquired
        //comm.CheckReturnMsg(); //Here boss

        //comm.SendMessage(new Point(1809, -166, 450));
        //comm.CheckReturnMsg(); //Target Acquired
        //comm.CheckReturnMsg(); //Here boss

        comm.SendMessage(new Point(1809, -166, 500), Rotation.Default);
        var send_mes = DateTime.Now;
        comm.CheckReturnMsg(); //Target Acquired
        var rec_mes = DateTime.Now;

        Console.WriteLine($"That took: {(rec_mes-send_mes).Milliseconds}");

        comm.CheckReturnMsg(); //Here boss

        //comm.SendMessage(false);
        //comm.CheckReturnMsg(); //Bye

        Console.ReadLine();
    }

    static Controller GetController(NetworkScannerSearchCriterias type)
    {
        NetworkScanner scanner = new NetworkScanner();
        //Assume single controller
        var controller_info = scanner.GetControllers(type)[0];
        var controller_id = controller_info.SystemId;
        Controller cntr = Controller.Connect(controller_id, ConnectionType.Standalone);
        Console.WriteLine("I found a controller named: {0}", controller_info.SystemName);
        return cntr;
    }

    public void SendMessage(bool boolMsg)
    {
        Byte[] data = null;

        var yes = "bool;TRUE\0";
        var no = "bool;FALSE\0";

        //Create message data
        if (boolMsg)
        {
            data = new UTF8Encoding().GetBytes(yes);
            Console.WriteLine(yes);
        }
        else
        {
            data = new UTF8Encoding().GetBytes(no);
            Console.WriteLine(no);
        }

        Console.WriteLine($"Data Size: {data.GetLength(0)}");

        //Place data and sender information in message
        sendExitMessage.SetData(data);
        sendExitMessage.Sender = TheOneAndOnlyQueue.QueueId;
        //Send message to the RAPID queue
        ROB_Queue.Send(sendExitMessage);
    }

    public void SendMessage(Point point, Rotation rot)
    {
        sendPositionMessage = new IpcMessage();

        //String string_to_send = $"robtarget;[[{point.x},{point.y},{point.z}],[0,1,0,0],[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]]";
        //String string_to_send = $"robtarget;[[{point.x.ToString().PadLeft(4, '0')},{point.y.ToString().PadLeft(4, '0')},{point.z.ToString().PadLeft(4, '0')}],[0,1,0,0],[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]]";

        String string_to_send = $"robtarget;[[{point.x},{point.y},{point.z}],[0,1,0,0],[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]]";
        int PaddingSize = MessageSize - string_to_send.Length;

        string_to_send = string_to_send.PadRight(PaddingSize, ' ');

        Console.WriteLine(string_to_send);      

        Byte[] data = new UTF8Encoding().GetBytes(string_to_send);
        Console.WriteLine($"target Length:  {data.GetLength(0)}" );
        //Place data and sender information in message
        sendPositionMessage.SetData(data);
        
        sendPositionMessage.Sender = TheOneAndOnlyQueue.QueueId;
        //Send message to the RAPID queue
        ROB_Queue.Send(sendPositionMessage);
    }

    //public void SendMessage(CustomDataTest custom_data)
    //{
    //    sendPositionMessage = new IpcMessage();

    //    String string_to_send = $"target_data;[[[{custom_data.x},{custom_data.y},{custom_data.z}],[0,1,0,0],[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]],[{custom_data.tcp}, {custom_data.ori}, {custom_data.leax}, {custom_data.reax}]]";
    //    int PaddingSize = MessageSize - string_to_send.Length;

    //    string_to_send = string_to_send.PadRight(PaddingSize, ' ');

    //    Console.WriteLine(string_to_send);      

    //    Byte[] data = new UTF8Encoding().GetBytes(string_to_send);
    //    Console.WriteLine($"target Length:  {data.GetLength(0)}" );
    //    //Place data and sender information in message
    //    sendPositionMessage.SetData(data);
        
    //    sendPositionMessage.Sender = TheOneAndOnlyQueue.QueueId;
    //    //Send message to the RAPID queue
    //    ROB_Queue.Send(sendPositionMessage);
    //}

    private void CheckReturnMsg()
    {
        returnMessage = new IpcMessage();

        IpcReturnType ret = IpcReturnType.Timeout;
        string answer = string.Empty;
        int timeout = 5000;
        //Check for msg in the PC SDK queue
        do
        {
            Console.WriteLine("Trying to get message");
            ret = TheOneAndOnlyQueue.Receive(timeout, returnMessage);
        } while (ret != IpcReturnType.OK);

        var string_length = returnMessage.UserDef+9;
        Console.WriteLine(string_length);
        answer = new UTF8Encoding().GetString(returnMessage.Data, 0, string_length);
        answer = answer.Replace("string;", "");

        Console.WriteLine(answer);
    }
}
