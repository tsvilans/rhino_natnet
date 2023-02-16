using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RhinoNatNet;

namespace NatNetMLClient_Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var Client = new RnnClient();
            Client.LocalIP = "10.10.4.23";
            Client.RemoteIP = "10.10.135.249";

            if (!Client.Connect())
            {
                return;
            }

            while (!(Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape))
            {
                Console.WriteLine("===============================================================================\n");
                var pts = Client.Markers();

                Console.WriteLine("{0} : {1}", pts.ToString(), pts.Length);

            }

            Client.Disconnect();
            /*  [NatNet] Disabling data handling function   */

        }
    }
}
