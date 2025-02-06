using BankerCore;

namespace BankerApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // Load configuration from file
            Config config = Config.Load();

            // Override config with command-line arguments if provided
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--ip" && i + 1 < args.Length) config.BankIp = args[++i];
                if (args[i] == "--port" && i + 1 < args.Length) config.Port = int.Parse(args[++i]);
                if (args[i] == "--storageFile" && i + 1 < args.Length) config.StorageFile = args[++i];
                if (args[i] == "--timeout" && i + 1 < args.Length) config.ClientTimeoutMs = int.Parse(args[++i]);
            }

            // Save updated config (optional)
            config.Save();

            // Start the bank server with loaded config
            BankServer server = new BankServer(
                bankIp: config.BankIp,
                port: config.Port,
                storageFile: config.StorageFile,
                clientTimeoutMs: config.ClientTimeoutMs,
                proxyTimeoutMs: config.ProxyTimeoutMs
            );

            server.Start();

            Console.WriteLine("Press ENTER to stop the server...");
            Console.ReadLine();
            server.Stop();
        }
    }
}
