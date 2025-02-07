using System.Text.Json;

namespace BankerCore
{
    /// <summary>
    /// Holds the local bank's accounts, balance info, and handles persistence.
    /// </summary>
    public class BankData
    {
        private readonly object _sync = new object();
        private Dictionary<int, long> _accounts;      // accountNumber -> balance
        private readonly string _storageFile;

        public BankData(string storageFile)
        {
            _storageFile = storageFile;
            _accounts = new Dictionary<int, long>();
            LoadData();
        }

        /// <summary>
        /// Loads account data from file (or creates empty if no file exists).
        /// Format:
        ///   accountNumber;balance
        ///   ...
        /// </summary>
        private void LoadData()
        {
            if (!File.Exists(_storageFile))
            {
                Logging.WriteLog("No existing storage file found. Starting fresh.");
                _accounts = new Dictionary<int, long>();
                return;
            }

            try
            {
                string json = File.ReadAllText(_storageFile);
                _accounts = JsonSerializer.Deserialize<Dictionary<int, long>>(json) ?? new Dictionary<int, long>();

                Logging.WriteLog($"Loaded {_accounts.Count} accounts from storage.");
            }
            catch (Exception ex)
            {
                Logging.WriteLog($"Error loading data file: {ex.Message}. Starting fresh.");
                _accounts = new Dictionary<int, long>();
            }
        }

        /// <summary>
        /// Saves the current account data to file.
        /// </summary>
        private void SaveData()
        {
            try
            {
                lock (_sync)
                {
                    string json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_storageFile, json);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLog($"Error saving data: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new account, returning its number. Must be [10000..99999].
        /// </summary>
        public int CreateAccount()
        {
            lock (_sync)
            {
                for (int candidate = 10000; candidate <= 99999; candidate++)
                {
                    if (!_accounts.ContainsKey(candidate))
                    {
                        _accounts[candidate] = 0L;
                        SaveData();
                        return candidate;
                    }
                }
            }
            throw new Exception("No available account numbers.");
        }

        /// <summary>
        /// Deposit an amount into an existing account.
        /// </summary>
        public void Deposit(int accountNumber, long amount)
        {
            lock (_sync)
            {
                if (!_accounts.ContainsKey(accountNumber))
                    throw new Exception("Account does not exist.");

                _accounts[accountNumber] += amount;
                SaveData();
            }
        }

        /// <summary>
        /// Withdraw an amount from an existing account.
        /// </summary>
        public void Withdraw(int accountNumber, long amount)
        {
            lock (_sync)
            {
                if (!_accounts.ContainsKey(accountNumber))
                    throw new Exception("Account does not exist.");

                long current = _accounts[accountNumber];
                if (current < amount)
                    throw new Exception("There are not enough funds.");

                _accounts[accountNumber] = current - amount;
                SaveData();
            }
        }

        /// <summary>
        /// Get the balance of an existing account.
        /// </summary>
        public long GetBalance(int accountNumber)
        {
            lock (_sync)
            {
                if (!_accounts.ContainsKey(accountNumber))
                    throw new Exception("Account does not exist.");

                return _accounts[accountNumber];
            }
        }

        /// <summary>
        /// Remove an account if the balance is zero.
        /// </summary>
        public void RemoveAccount(int accountNumber)
        {
            lock (_sync)
            {
                if (!_accounts.ContainsKey(accountNumber))
                    throw new Exception("Account does not exist.");

                if (_accounts[accountNumber] != 0)
                    throw new Exception("It is not possible to delete the bank account on which there are funds.");

                _accounts.Remove(accountNumber);
                SaveData();
            }
        }

        /// <summary>
        /// Returns the total sum in the bank.
        /// </summary>
        public long GetTotalAmount()
        {
            lock (_sync)
            {
                long sum = 0;
                foreach (var bal in _accounts.Values)
                {
                    sum += bal;
                }
                return sum;
            }
        }

        /// <summary>
        /// Returns the number of clients (accounts).
        /// </summary>
        public int GetNumberOfClients()
        {
            lock (_sync)
            {
                return _accounts.Count;
            }
        }
    }
}
