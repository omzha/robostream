using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using System;

namespace ControllerAPI
{
    /// <summary>
    /// Creates a controller instance from various sources.
    /// </summary>
    class Create
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [MTAThread]
        static void Main(string[] args)
        {
            Controller controller = GetController(NetworkScannerSearchCriterias.Virtual);

            DoSomething(controller);

            Console.WriteLine("Press any key to terminate");
            Console.ReadKey();
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

        static void DoSomething(Controller ctrl)
        {
            ctrl.Logon(UserInfo.DefaultUser);

            var rd = ctrl.Rapid.GetTask("T_ROB1").GetModule("MainModule").GetRapidData("boby");

            Console.WriteLine("Boby before: {0}", rd.Value.ToString());

            if (ctrl.OperatingMode == ControllerOperatingMode.Auto)
            {
                using (Mastership.Request(ctrl))
                {
                    if (rd.Value is ABB.Robotics.Controllers.RapidDomain.Num)
                    {
                        ABB.Robotics.Controllers.RapidDomain.Num num = (ABB.Robotics.Controllers.RapidDomain.Num)rd.Value;
                        num.FillFromNum(15.0);
                        rd.Value = num;
                    }
                }
            }

            Console.WriteLine("Boby after: {0}", rd.Value.ToString());
        }
    }
}
