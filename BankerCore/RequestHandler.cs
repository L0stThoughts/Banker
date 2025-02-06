using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BankerCore
{
    /// <summary>
    /// Parses incoming commands and either handles them locally or proxies them to other banks.
    /// </summary>
    public class RequestHandler
    {
        private readonly string _localBankIp;
        private readonly BankData _bankData;
        private readonly int _proxyTimeoutMs;

        public RequestHandler(string localBankIp, BankData bankData, int proxyTimeoutMs)
        {
            _localBankIp = localBankIp;
            _bankData = bankData;
            _proxyTimeoutMs = proxyTimeoutMs;
        }

        /// <summary>
        /// Main method to process a single line command.
        /// </summary>
        public string ProcessCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return "ER Empty command";

            // e.g. "AD 12345/10.1.2.4 3000"
            var parts = commandLine.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "ER Command not recognized";

            string cmd = parts[0].ToUpper();

            return cmd switch
            {
                "BC" => HandleBC(),
                "AC" => HandleAC(),
                "AD" => HandleADOrAW(isDeposit: true, parts),
                "AW" => HandleADOrAW(isDeposit: false, parts),
                "AB" => HandleAB(parts),
                "AR" => HandleAR(parts),
                "BA" => HandleBA(),
                "BN" => HandleBN(),
                _ => "ER Unknown command"
            };
        }

        private string HandleBC()
        {
            try
            {
                return $"BC {_localBankIp}";
            }
            catch (Exception ex)
            {
                return $"ER {ex.Message}";
            }
        }

        private string HandleAC()
        {
            try
            {
                int newAcct = _bankData.CreateAccount();
                return $"AC {newAcct}/{_localBankIp}";
            }
            catch (Exception ex)
            {
                return $"ER {ex.Message}";
            }
        }

        private string HandleADOrAW(bool isDeposit, string[] parts)
        {
            if (parts.Length < 3)
            {
                return "ER Missing arguments. Usage: AD <account>/<ip> <number>";
            }

            // parts[1] = "12345/10.1.2.4"
            // parts[2] = "3000"
            string accountIp = parts[1];
            string amountStr = parts[2];

            if (!accountIp.Contains("/"))
            {
                return "ER Format of the account number is not correct.";
            }

            var accountIpSplit = accountIp.Split('/');
            if (accountIpSplit.Length != 2)
            {
                return "ER Format of the account number is not correct.";
            }

            string accountStr = accountIpSplit[0];
            string bankIp = accountIpSplit[1];

            if (!int.TryParse(accountStr, out int accountNumber))
            {
                return "ER The account number of the banking account and the amount aren't in the correct format.";
            }

            if (!long.TryParse(amountStr, out long amount))
            {
                return "ER The account number of the banking account and the amount aren't in the correct format.";
            }

            if (bankIp == _localBankIp)
            {
                try
                {
                    if (isDeposit)
                    {
                        _bankData.Deposit(accountNumber, amount);
                        return "AD"; // success
                    }
                    else
                    {
                        _bankData.Withdraw(accountNumber, amount);
                        return "AW"; // success
                    }
                }
                catch (Exception ex)
                {
                    return $"ER {ex.Message}";
                }
            }
            else
            {
                // Proxy call
                string forwardCommand = isDeposit
                    ? $"AD {accountNumber}/{bankIp} {amount}"
                    : $"AW {accountNumber}/{bankIp} {amount}";

                return ProxyToRemote(bankIp, forwardCommand);
            }
        }

        private string HandleAB(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "ER Missing arguments. Usage: AB <account>/<ip>";
            }

            string accountIp = parts[1];
            if (!accountIp.Contains("/"))
            {
                return "ER Format of the account number is not correct.";
            }

            var tokens = accountIp.Split('/');
            if (tokens.Length != 2)
            {
                return "ER Format of the account number is not correct.";
            }

            if (!int.TryParse(tokens[0], out int acctNumber))
            {
                return "ER Format of the account number is not correct.";
            }

            string bankIp = tokens[1];

            if (bankIp == _localBankIp)
            {
                try
                {
                    long balance = _bankData.GetBalance(acctNumber);
                    return $"AB {balance}";
                }
                catch (Exception ex)
                {
                    return $"ER {ex.Message}";
                }
            }
            else
            {
                // proxy
                string forwardCommand = $"AB {acctNumber}/{bankIp}";
                return ProxyToRemote(bankIp, forwardCommand);
            }
        }

        private string HandleAR(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "ER Missing arguments. Usage: AR <account>/<ip>";
            }

            string accountIp = parts[1];
            if (!accountIp.Contains("/"))
            {
                return "ER Format of the account number is not correct.";
            }

            var tokens = accountIp.Split('/');
            if (tokens.Length != 2)
            {
                return "ER Format of the account number is not correct.";
            }

            if (!int.TryParse(tokens[0], out int acctNumber))
            {
                return "ER Format of the account number is not correct.";
            }

            string bankIp = tokens[1];

            if (bankIp == _localBankIp)
            {
                try
                {
                    _bankData.RemoveAccount(acctNumber);
                    return "AR";
                }
                catch (Exception ex)
                {
                    return $"ER {ex.Message}";
                }
            }
            else
            {
                // proxy
                string forwardCommand = $"AR {acctNumber}/{bankIp}";
                return ProxyToRemote(bankIp, forwardCommand);
            }
        }

        private string HandleBA()
        {
            try
            {
                long total = _bankData.GetTotalAmount();
                return $"BA {total}";
            }
            catch (Exception ex)
            {
                return $"ER {ex.Message}";
            }
        }

        private string HandleBN()
        {
            try
            {
                int count = _bankData.GetNumberOfClients();
                return $"BN {count}";
            }
            catch (Exception ex)
            {
                return $"ER {ex.Message}";
            }
        }

        /// <summary>
        /// Proxies the given command to a remote bank node at the specified IP
        /// and returns the remote's response.
        /// </summary>
        private string ProxyToRemote(string remoteIp, string command)
        {
            int remotePort = 65530;

            try
            {
                using TcpClient client = new TcpClient();
                client.ReceiveTimeout = _proxyTimeoutMs;
                client.SendTimeout = _proxyTimeoutMs;

                client.Connect(IPAddress.Parse(remoteIp), remotePort);

                using NetworkStream ns = client.GetStream();
                using var sr = new System.IO.StreamReader(ns, Encoding.UTF8);
                using var sw = new System.IO.StreamWriter(ns, new UTF8Encoding(false))
                {
                    AutoFlush = true
                };

                sw.WriteLine(command);
                string response = sr.ReadLine();

                return response ?? $"ER No response from {remoteIp}";
            }
            catch (Exception ex)
            {
                return $"ER Proxy error: {ex.Message}";
            }
        }
    }
}