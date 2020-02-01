namespace Azurlane
{
    public class Utils
    {
        internal static void Command(string argument, string workingDirectory = null)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "cmd";
                process.StartInfo.Arguments = $"/c {argument}";
                process.StartInfo.WorkingDirectory = workingDirectory ?? PathMgr.Thirdparty();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();
            }
        }

        internal static void LogDebug(string message, bool space, bool writeLine, params object[] arg)
        {
            Write($@"[{DateTime.Now:HH:mm}][DEBUG]> {message}", space, writeLine, arg);
        }

        internal static void LogException(string message, Exception exception)
        {
            // Send a debug message to terminal
            LogDebug(message, true, true);

            // Checking whether Logs.txt is in local folder
            if (!File.Exists(PathMgr.Local("Logs.txt")))
                // Create an empty Logs.txt if file not exists
                File.WriteAllText(PathMgr.Local("Logs.txt"), string.Empty);

            try
            {
                using (var streamWriter = new StreamWriter(PathMgr.Local("Logs.txt"), true))
                {
                    streamWriter.WriteLine(
                        "=== START ==============================================================================");
                    streamWriter.WriteLine(message);
                    streamWriter.WriteLine($"Date: {DateTime.Now.ToString()}");
                    streamWriter.WriteLine($"Exception Message: {exception.Message}");
                    streamWriter.WriteLine($"Exception StackTrace: {exception.StackTrace}");
                    streamWriter.WriteLine(
                        "=== END ================================================================================");
                    streamWriter.WriteLine();
                }
            }
            catch (Exception)
            {
                // Empty
            }
        }

        internal static void LogInfo(string message, bool space, bool writeLine, params object[] arg)
        {
            Write($@"[{DateTime.Now:HH:mm}][INFO]> {message}", space, writeLine, arg);
        }

        internal static void Write(string message, bool space, bool writeLine, params object[] arg)
        {
            Console.Write((space ? @"  " : null) + message, arg);
            if (writeLine)
                Console.WriteLine();
        }
    }
}