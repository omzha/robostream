using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [MTAThread]
    static void Main(string[] args)
    {
        var path = "C:\\Users\\Oliver.Moldow\\Downloads\\point stream_03.txt";

        var pattern = @"[/s]*MoveL \[\[([\d., -]+)\], \[([\d., -]+)\]";

        Regex rg = new Regex(pattern);

        List<Target> targets = new List<Target>();

        // Read in the list of points
        var lines = File.ReadLines(path);
        foreach (var line in lines)
        {
            var match = rg.Match(line);
            if(match.Success)
            {
                var pos_str = match.Groups[1].Captures[0].ToString();
                var ort_str = match.Groups[2].Captures[0].ToString();

                //Console.WriteLine($"[{pos_str}], [{ort_str}]");

                targets.Add(new Target(ParsePoint(ref pos_str), ParseOrt(ref ort_str)));

                //Console.WriteLine(targets[targets.Count - 1].GetMessage());
            }
        }

        RAB_COM com = new RAB_COM(ControllerType.Real);

        Console.WriteLine("I connected to {0}", com.Controller.SystemName);

        com.TargetSent += OnTargetSent;
        com.TargetReceived += OnTargetReceived;
        com.TargetReached += OnTargetReached;

        int count = 0;

        foreach (var tar in targets)
        {
            com.SendMessage(tar);
            Console.WriteLine($"{++count} of {targets.Count} - {(((float)count / targets.Count) * 100.0).ToString("F3")}%");
        }

        com.SendExitMessage();
    }

    static void OnTargetSent(object sender, TargetEventArgs e)
    {
        Console.WriteLine($"Sent Target: {e.Target}");
    }

    static void OnTargetReceived(object sender, TargetEventArgs e)
    {
        Console.WriteLine($"Received Target: {e.Target}");
    }

    static void OnTargetReached(object sender, TargetEventArgs e)
    {
        Console.WriteLine($"Reached Target: {e.Target}");
        Console.WriteLine();
        Console.WriteLine();
    }

    static public Point ParsePoint(ref String input)
    {
        var strings = input.Split(',');
        return new Point(float.Parse(strings[0].Trim()), float.Parse(strings[1].Trim()), float.Parse(strings[2].Trim()));
    }

    static public Rotation ParseOrt(ref String input)
    {
        var strings = input.Split(',');
        return new Rotation(float.Parse(strings[0].Trim()), float.Parse(strings[1].Trim()), float.Parse(strings[2].Trim()),float.Parse(strings[3].Trim()));
    }

    // static void OnTargetSent(object sender, EventArgs e)
    // {
    //     Console.WriteLine("Event Target Sent");
    // }
    //
    // static void OnTargetReached(object sender, EventArgs e)
    // {
    //     Console.WriteLine("Event Target Reached");
    // }
}
