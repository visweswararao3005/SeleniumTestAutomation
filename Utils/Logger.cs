using System.Data.SqlClient;

namespace TestAutomation.Utils
{
    public static class Logger
    {
        private static readonly string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string logFile = Path.Combine(logDirectory, $"Log_{DateTime.Now:dd_MM_yyyy}.txt");

        static Logger()
        {
            // Ensure "Logs" folder exists
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        private static void WriteLog(string level, string message, ConsoleColor color)
        {
            string logEntry = $"[{level}] {DateTime.Now:HH:mm:ss} - {message}";

            // Write to console
            Console.ForegroundColor = color;
            Console.WriteLine(logEntry);
            Console.ResetColor();

            // Write to file (append mode)
            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message, ConsoleColor.Green);
        }

        public static void Error(string message)
        {
            WriteLog("ERROR", message, ConsoleColor.Red);
        }

        public static void Warn(string message)
        {
            WriteLog("WARN", message, ConsoleColor.Yellow);
        }
    }

    public static class StatusLogger
    {
        private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "Status Logs");
        private static readonly string LogFile = Path.Combine(LogDir, $"Status_{DateTime.Now:dd_MM_yyyy}.txt");
        private static readonly object _lock = new object();
        private static bool _runtimeLogged = false;

        static StatusLogger()
        {
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);
        }

        public static void LogRuntimeStart()
        {
            lock (_lock)
            {
                if (!_runtimeLogged) // avoid duplicate runtime logs in same run
                {
                    File.AppendAllText(LogFile, $"\n\nRuntime Start: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                    File.AppendAllText(LogFile, $"Client  -  StartTime  -  Status  -  TimeTaken  -  Screen  -  TestName\n");
                    _runtimeLogged = true;
                }
            }
        }

        public static void LogTestStatus(string testName, DateTime startTime, DateTime endTime, string status, string Screen, string ClientName)
        {
            lock (_lock)
            {
                var timeTaken = endTime - startTime;
                string line = $"{ClientName}  -  {startTime:HH:mm:ss}  -  {status}  -  {timeTaken.TotalSeconds:F2}s  -  {Screen}  -  {testName}";
                File.AppendAllText(LogFile, line + Environment.NewLine);
            }
        }
    }

    public static class DbLogger
    {
        public static void LogTestResult(string testId,string testName, DateTime start, DateTime end, string status, string Screen, string ClientName)
        { 
            // Dynamically resolve the connection string for this client
            var _connString = Config.Get($"AppSettings:{ClientName}");
            if (string.IsNullOrEmpty(_connString))
            {
                Logger.Error($"No connection string found for client: {ClientName}");
                return;
            }
            var duration = (int)(end - start).TotalSeconds;

            using (var conn = new SqlConnection(_connString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO TestRunResults 
                                (TestID, TestName, StartTime, EndTime, DurationSeconds, Status, ClientName, Screen) 
                                VALUES (@TestID,@TestName, @StartTime, @EndTime, @Duration, @Status, @ClientName, @Screen)";
                cmd.Parameters.AddWithValue("@TestID", testId);
                cmd.Parameters.AddWithValue("@TestName", testName);
                cmd.Parameters.AddWithValue("@StartTime", start);
                cmd.Parameters.AddWithValue("@EndTime", end);
                cmd.Parameters.AddWithValue("@Duration", duration);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@ClientName", ClientName);
                cmd.Parameters.AddWithValue("@Screen", Screen);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }

}
