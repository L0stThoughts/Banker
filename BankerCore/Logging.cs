namespace BankerCore
{
    /// <summary>
    /// Simple logging utility for the Banker system.
    /// </summary>
    public static class Logging
    {
        private static readonly object _sync = new object();
        private static readonly string _logFile = "banker.log";

        public static void WriteLog(string message)
        {
            lock (_sync)
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                Console.WriteLine(line);
                File.AppendAllText(_logFile, line + Environment.NewLine);
            }
        }
    }
}
