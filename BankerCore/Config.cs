using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace BankerCore
{
    public class Config
    {
        public string BankIp { get; set; } = GetLocalIPAddress(); // Server's local IP
        public int Port { get; set; } = 65530;                   // Port for server communication
        public string StorageFile { get; set; } = "bankdata.txt"; // Path to the storage file
        public int ClientTimeoutMs { get; set; } = 5000;         // Client timeout in milliseconds
        public int ProxyTimeoutMs { get; set; } = 5000;          // Proxy timeout in milliseconds

        private static readonly string ConfigFilePath = "config.json";

        /// <summary>
        /// Loads configuration from config.json, or creates a default one if not found.
        /// </summary>
        public static Config Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<Config>(json) ?? new Config();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading config: {ex.Message}, using defaults.");
                }
            }
            else
            {
                Console.WriteLine("Config file not found, creating default config.");
                Config defaultConfig = new Config();
                defaultConfig.Save();
                return defaultConfig;
            }

            return new Config();
        }

        /// <summary>
        /// Saves the current configuration to config.json.
        /// </summary>
        public void Save()
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }

        /// <summary>
        /// Retrieves the local IP address (excluding 127.0.0.1) by connecting to an external server.
        /// This is used as the server's IP.
        /// </summary>
        public static string GetLocalIPAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 80); // Google's public DNS server
                    if (socket.LocalEndPoint is IPEndPoint endPoint)
                    {
                        return endPoint.Address.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving local IP: {ex.Message}");
            }

            return "127.0.0.1"; // Fallback if no valid IP is found
        }

        /// <summary>
        /// Validates the server port number to ensure it's within the acceptable range.
        /// </summary>
        public bool IsValidPort()
        {
            return Port >= 1024 && Port <= 65535;
        }
    }
}
