using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadeInTheUSB.Web;
using System.Reflection;

namespace MadeInTheUSB.WebServerNS.ConsoleNS
{
    class Program
    {
        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attribs.Length > 0)
                return ((AssemblyProductAttribute)attribs[0]).Product;
            return null;
        }


        static void Main(string[] args)
        {
            System.Console.WriteLine(GetAssemblyProduct());

            var wsc = new WebServerController();
            wsc.StartWebServer(WebServer.GetCommandLinePort());

            Console.WriteLine("Hit enter key to stop the web server");
            var k = Console.ReadKey();

            wsc.StopWebServer();
        }
    }
}
