using System.Net;
using System.Net.Sockets;

namespace BankerCore
{
    /// <summary>
    /// Manages the network listening, client handling, and references
    /// to the BankData and configuration.
    /// </summary>
    public class BankServer
    {
        private readonly string _bankIp;            // The IP address this server identifies with
        private readonly int _port;                 // TCP Port to listen on
        private readonly int _clientTimeoutMs;      // Timeout for handling a single client
        private readonly int _proxyTimeoutMs;       // Timeout for proxy calls
        private readonly TcpListener _listener;
        private CancellationTokenSource _cts;       // For graceful shutdown

        private readonly BankData _bankData;        // Holds accounts and handles persistence

        public BankServer(
            string bankIp,
            int port,
            string storageFile,
            int clientTimeoutMs,
            int proxyTimeoutMs)
        {
            _bankIp = bankIp;
            _port = port;
            _clientTimeoutMs = clientTimeoutMs;
            _proxyTimeoutMs = proxyTimeoutMs;

            // Initialize data storage/persistence
            _bankData = new BankData(storageFile);

            // Create the TCP listener
            if (!IPAddress.TryParse(bankIp, out IPAddress ipAddress))
            {
                ipAddress = IPAddress.Any;
            }

            _listener = new TcpListener(ipAddress, _port);
        }

        /// <summary>
        /// Start listening on the configured IP and port.
        /// </summary>
        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener.Start();
            Logging.WriteLog($"Server started on {_bankIp}:{_port}");

            Task.Run(() => AcceptClientsAsync(_cts.Token));
        }

        /// <summary>
        /// Stop the server gracefully.
        /// </summary>
        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();
            Logging.WriteLog("Server stopped.");
        }

        /// <summary>
        /// Accepts incoming connections asynchronously and spawns a handler for each.
        /// </summary>
        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client, token);
                }
                catch (ObjectDisposedException)
                {
                    // listener has been stopped
                    break;
                }
                catch (Exception ex)
                {
                    Logging.WriteLog($"Error accepting client: {ex.Message}");
                }
            }
        }

        private static string GetHelpMessage(string clientIp)
        {
            return $@"
                Welcome Client {clientIp}!
                Available Commands (One per line):
                  BC                Returns bank code (IP)
                  AC                Create new account
                  AD <acct>/<ip> <amount>  Deposit to account
                  AW <acct>/<ip> <amount>  Withdraw from account
                  AB <acct>/<ip>           Get balance of account
                  AR <acct>/<ip>           Remove account (if zero)
                  BA                       Get total bank amount
                  BN                       Get number of accounts
                  exit                     Disconnect from server
                  help / ?                 Display this help

                NOTE: If <ip> differs from the server IP, the command is proxied.
            ";
        }

        /// <summary>
        /// Handles interaction with a single client: read command, process, respond.
        /// </summary>
        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            client.ReceiveTimeout = _clientTimeoutMs;
            client.SendTimeout = _clientTimeoutMs;

            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint)?.Address.ToString() ?? "Unknown";

            Logging.WriteLog($"Client connected: {clientIp}");

            try
            {
                using NetworkStream stream = client.GetStream();
                using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false))
                {
                    AutoFlush = true
                };

                // 1) Send a welcome screen / usage guide
                await writer.WriteLineAsync("************************************************");
                await writer.WriteLineAsync("*  Welcome to the Banker                       *");
                await writer.WriteLineAsync($"*  Your IP: {clientIp}");
                await writer.WriteLineAsync("*  Type '?' or 'help' to see available commands*");
                await writer.WriteLineAsync("*  Type 'exit' to close the connection         *");
                await writer.WriteLineAsync("************************************************");
                await writer.WriteLineAsync();

                // 2) Process commands in a loop until user disconnects or types 'exit'
                while (true)
                {
                    // Prompt user for input (optional)
                    await writer.WriteAsync("> ");
                    string line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        // This means the client closed the connection
                        Logging.WriteLog($"Client {clientIp} disconnected (no data).");
                        break;
                    }

                    // Check for 'exit' command
                    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("Goodbye!");
                        Logging.WriteLog($"Client {clientIp} requested exit.");
                        break;
                    }

                    // Check for 'help' or '?'
                    if (line.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                        line.Equals("?", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync(GetHelpMessage(clientIp));
                        continue;
                    }

                    Logging.WriteLog($"Received from {clientIp}: {line}");

                    // Process the banking command
                    var handler = new RequestHandler(_bankIp, _bankData, _proxyTimeoutMs);
                    string response = handler.ProcessCommand(line);

                    Logging.WriteLog($"Responding to {clientIp}: {response}");
                    await writer.WriteLineAsync(response);
                }
            }
            catch (IOException ioex)
            {
                // Typically a timeout or connection lost
                Logging.WriteLog($"IOException from {clientIp}: {ioex.Message}");
            }
            catch (Exception ex)
            {
                Logging.WriteLog($"Error handling client {clientIp}: {ex}");
            }
            finally
            {
                client.Close();
                Logging.WriteLog($"Client disconnected: {clientIp}");
            }
        }
    }
}
