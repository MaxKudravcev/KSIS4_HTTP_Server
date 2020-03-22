using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleHTTPServer server;
            string path;

            Console.WriteLine("Specify the folder with your web-application:");
            path = Console.ReadLine();
            Console.WriteLine("Enter '/exit' to close the server...\n");

            server = new SimpleHTTPServer(path);

            while (Console.ReadLine() != "/exit")
                Console.WriteLine("Enter '/exit' to close the server...");

            server.Stop();
        }
    }
}
