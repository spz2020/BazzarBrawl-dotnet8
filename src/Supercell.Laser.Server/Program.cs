namespace Supercell.Laser.Server
{
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Handler;
    using Supercell.Laser.Server.Networking;
    using Supercell.Laser.Server.Settings;
    using Supercell.Laser.Titan.Debug;
    using System.Drawing;
    using System.Runtime.InteropServices;

    static class Program
    {
        public static string SERVER_VERSION = File.ReadAllText("version.txt").Trim();

        public static string BUILD_TYPE = "STAGE"; // хз мне чета лень через кфг делать

        private static void Main(string[] args)
        {
            Console.Title = "Bazzar Brawl Server Emulator";
            Console.WindowWidth = 150;
            Console.WindowHeight = 30;
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            Colorful.Console.WriteWithGradient(
                @"

                               ██████╗  █████╗ ███████╗███████╗ █████╗ ██████╗     ██████╗ ██████╗  █████╗ ██╗    ██╗██╗     
                               ██╔══██╗██╔══██╗╚══███╔╝╚══███╔╝██╔══██╗██╔══██╗    ██╔══██╗██╔══██╗██╔══██╗██║    ██║██║     
                               ██████╔╝███████║  ███╔╝   ███╔╝ ███████║██████╔╝    ██████╔╝██████╔╝███████║██║ █╗ ██║██║     
                               ██╔══██╗██╔══██║ ███╔╝   ███╔╝  ██╔══██║██╔══██╗    ██╔══██╗██╔══██╗██╔══██║██║███╗██║██║     
                               ██████╔╝██║  ██║███████╗███████╗██║  ██║██║  ██║    ██████╔╝██║  ██║██║  ██║╚███╔███╔╝███████╗
                               ╚═════╝ ╚═╝  ╚═╝╚══════╝╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝    ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝ ╚══╝╚══╝ ╚══════╝
                                                                                                                                                       
                                                                                           " + "\n\n\n", Color.LightSalmon, Color.DarkRed, 8);

            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Cmd,
            "Laser.Server initialized");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Cmd,
            "Laser.Logic initialized");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Cmd,
            "Laser.Titan initialized");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Info,
                        $"Runtime version: {RuntimeInformation.FrameworkDescription}");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Info,
            $"OS: {RuntimeInformation.OSDescription}");


            Logger.Init();
            Configuration.Instance = Configuration.LoadFromFile("config.json");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Cmd,
            "Config initialized");

            Resources.InitDatabase();
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Info,
            "Database initialized");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Info,
            $"Accounts cached: {Accounts.GetAccountCount()}");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Info,
            $"Alliances cached: {Alliances.GetAllianceCount()}");
            Resources.InitLogic();
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Load,
            $"Fingerprint {Fingerprint.Version} loaded!");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Load,
            $"DataTables {DataTables.Gamefiles.Count} loaded!");
            Resources.InitNetwork();

            UDPGateway.Init("0.0.0.0", Configuration.Instance.UdpPort);
            TCPGateway.Init("0.0.0.0", 9339);

            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Udp,
            $"UDP Server started at 0.0.0.0:{Configuration.Instance.UdpPort}, 9449, 9023, 9003, 9005");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Tcp,
            $"TCP Server started at 0.0.0.0:9339");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Info,
            $"ServerBuild: {SERVER_VERSION}");
            ConsoleLogger.WriteTextWithPrefix(ConsoleLogger.Prefixes.Info,
            $"Server env: {BUILD_TYPE}");

            ExitHandler.Init();
            CmdHandler.Start();
        }
    }
}