using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GlassServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SimManager.Connect();

            var httpThread = new Thread(() => CreateHostBuilder(args).Build().Run());
            httpThread.IsBackground = true;
            httpThread.Start();

            var sockServer = new SocketServer();
            sockServer.Start();

            Console.WriteLine("Running! Press any key to exit.");
            Console.ReadKey(true);
            Console.WriteLine("Exiting...");
            sockServer.Stop();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
