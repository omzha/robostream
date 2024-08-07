using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
//using ABB.Robotics.Controllers.RapidDomain;
using System;
using System.Text;
using System.Diagnostics;


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
    private byte ReturnMessageCount = 0;

    // Target sent event
    public event EventHandler<TargetEventArgs> TargetSent;

    protected virtual void OnTargetSent(TargetEventArgs e)
    {
        TargetSent?.Invoke(this, e);
    }

    // Target received event
    public event EventHandler<TargetEventArgs> TargetReceived;

    protected virtual void OnTargetReceived(TargetEventArgs e)
    {
        TargetReceived?.Invoke(this, e);
    }

    // Target reached event
    public event EventHandler<TargetEventArgs> TargetReached;

    protected virtual void OnTargetReached(TargetEventArgs e)
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

        // Set message data
        this.PositionMessage.SetData(data);

        // Set message sender
        this.PositionMessage.Sender = SDK_Queue.QueueId;

        // Send message to the RAPID queue
        Robot_Queue.Send(this.PositionMessage);
        
        // OnTargetSent
        OnTargetSent(new TargetEventArgs {Target = target.GetMessage()});

        // OnTargetReceived
        CheckReturnMsg();
        OnTargetReceived(new TargetEventArgs {Target = target.GetMessage()});

        // OnTargetReached
        CheckReturnMsg();
        OnTargetReached(new TargetEventArgs {Target = target.GetMessage()});
    }

    private void CheckReturnMsg()
    {
        this.ReturnMessage = new IpcMessage();

        IpcReturnType ret = IpcReturnType.Timeout;
        string answer = string.Empty;
        int timeout = 5000;
        //Check for msg in the PC SDK queue
        do
        {
            //Console.WriteLine("Trying to get message");
            ret = SDK_Queue.Receive(timeout, this.ReturnMessage);
        } while (ret != IpcReturnType.OK);

        var string_length = this.ReturnMessage.UserDef+9;
        answer = new UTF8Encoding().GetString(this.ReturnMessage.Data, 0, string_length);
        answer = answer.Replace("string;", "");

#if DEBUG
        Console.WriteLine(answer);
#endif
    }
}
