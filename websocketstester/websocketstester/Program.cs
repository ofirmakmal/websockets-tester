using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Runtime.Loader;
using WebSocketSharp.Server;
using WebSocketSharp;

namespace websocketstester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 3)
            {
                System.Console.WriteLine($"Error: Invalid arguments: 'websocketstester mode port sleep'");
                Environment.Exit(1);
            }

            var mode = args[0];
            var config = new Config(args.Skip(1).ToArray());

            if (mode.Trim().Equals("client", StringComparison.InvariantCultureIgnoreCase))
            {
                await RunWebSocketsClient(config);
            }
            else if (mode.Trim().Equals("server", StringComparison.InvariantCultureIgnoreCase))
            {
                await RunWebSocketsServer(config);
            }
            else if (mode.Trim().Equals("master", StringComparison.InvariantCultureIgnoreCase))
            {
                await RunWebSocketsMaster(config);
            }
            else
            {
                System.Console.WriteLine($"Error: Unsupported mode {mode}");
                Environment.Exit(1);
            }
        }

        private async static Task RunWebSocketsMaster(Config config)
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Console.WriteLine("Exiting!");

            Console.WriteLine("Running WebSockets client and server testers:");

            Func<string, bool, DataReceivedEventHandler> reportLog = (who, isHighlight) => (sender, e) => Log(who, e.Data, isHighlight);
            void Log(string who, string what, bool isHighlight = false)
            {
                Console.ForegroundColor = isHighlight ? ConsoleColor.Red : ConsoleColor.Gray;

                var when = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture);
                System.Console.WriteLine($"{when}: {who}-{what}");
            }

            var execFilePath = Assembly.GetExecutingAssembly().Location;

            Process SpawnChild(string mode, string arguments)
            {
                var childProcess = Process.Start(new ProcessStartInfo("dotnet", $"{execFilePath} {mode} {arguments}") { RedirectStandardOutput = true, RedirectStandardError = true });
                childProcess.OutputDataReceived += reportLog(mode, false);
                childProcess.ErrorDataReceived += reportLog(mode, true);
                childProcess.BeginOutputReadLine();
                childProcess.BeginErrorReadLine();

                return childProcess;
            }

            var childArgs = $"{config.Port} {config.Sleep}";
            var clients = 5;
            var processes = new List<Process>();
            var serverProcess = SpawnChild("server", childArgs);
            processes.Add(serverProcess);

            // Add clients
            for (int i = 0; i < clients; i++)
            {
                processes.Add(SpawnChild("client", childArgs));
            }

            var processTasks = processes.Select(p => Task.Run(() => p.WaitForExit()));

            // Wait until any of the processes are killed, or the master is killed
            await Task.WhenAny(processTasks);

            void KillSubprocesses()
            {
                // Make sure both the client process are killed
                try
                {
                    for (int i = 0; i < processes.Count -1; i++)
                    {
                        processes[i].Kill();
                        System.Console.WriteLine("Process killed.");
                    }
                }
                catch (Exception ex) { }
            }

            KillSubprocesses();
            Environment.Exit(0);
        }

        private async static Task RunWebSocketsClient(Config config)
        {
            System.Console.WriteLine("Waiting for a second for the server to be ready.");

            await Task.Delay(1000);

            System.Console.WriteLine($"Connecting to server on port {config.Port}");
            // Todo: Add WS client code
            using (var ws = new WebSocket($"ws://127.0.0.1:{config.Port}/latency"))
            {
                ws.OnMessage += (sender, e) =>
                {
                    var ticks = long.Parse(e.Data);
                    Console.WriteLine($"Response: { Math.Round(TimeSpan.FromTicks(DateTime.Now.Ticks - ticks).TotalMilliseconds, 2)} ms");
                };

                ws.Connect();
                while (true)
                {
                    await Task.Delay(config.Sleep);
                    System.Console.WriteLine("Sent!");
                    ws.Send(DateTime.Now.Ticks.ToString());
                }
            }
        }

        private static async Task RunWebSocketsServer(Config config)
        {
            System.Console.WriteLine($"Start listening on port {config.Port}");
            // Todo: Add WS server code
            var socketsServer = new WebSocketServer("ws://0.0.0.0:" + config.Port) { AllowForwardedRequest = true };
            socketsServer.AddWebSocketService<EchoWebSocketsServer>("/latency");
            socketsServer.Start();

            Console.ReadLine();
        }
    }

    class Config
    {
        public int Port { get; set; }
        public int Sleep { get; set; }
        public Config(string[] args)
        {
            Port = int.Parse(args[0]);
            Sleep = int.Parse(args[1]);
        }
    }
}
