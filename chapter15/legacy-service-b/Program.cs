using Microsoft.Owin.Hosting;
using System;

namespace LegacyServiceB
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var propagationMode = GetMode(args);
            Console.WriteLine($"Starting service-b with context propagation mode - {propagationMode}");

            var startup = new Startup(propagationMode);
            using (WebApp.Start("http://localhost:5050", appBuilder => startup.Configuration(appBuilder)))
            {
                Console.ReadLine();
            }
        }

        private static ContextPropagationMode GetMode(string[] args)
        {
            string mode = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--correlation-mode")
                {
                    mode = args[i + 1];
                    break;
                }
            }

            if (mode == "pass-through") return ContextPropagationMode.TraceContextPassThrough;
            if (mode == "correlation-id") return ContextPropagationMode.CorrelationId;

            return ContextPropagationMode.None;
        }
    }
}