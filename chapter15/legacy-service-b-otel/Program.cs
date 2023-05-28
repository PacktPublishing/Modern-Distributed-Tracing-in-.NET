using Microsoft.Owin.Hosting;
using System;

namespace LegacyServiceBOTel
{
    internal class Program
    {
        static void Main(string[] _)
        {
            Console.WriteLine($"Hello from service B running on {Environment.Version}");
            using (WebApp.Start<Startup>(url: "http://localhost:5050"))
            {
                Console.ReadLine();
            }
        }
    }
}