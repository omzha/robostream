using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [MTAThread]
    static void Main(string[] args)
    {
        RAB_COM comm = new RAB_COM(ControllerType.Real);

        comm.TargetReached+=OnTargetReached;

        comm.SendMessage(new Target(new Point(1809, -166, 1248)));
        comm.CheckReturnMsg(); // Target received
        comm.CheckReturnMsg(); // Arrived at target

        comm.SendExitMessage();
        comm.CheckReturnMsg();

        //Pause before exit
        Console.ReadLine();
    }

    static void OnTargetReached(object sender, EventArgs e)
    {
        Console.WriteLine("Target Reached");
    }
}
