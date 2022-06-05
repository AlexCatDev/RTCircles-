using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Easy2D
{
    public enum LogLevel
    {
        /// <summary>
        /// Cyan text
        /// </summary>
        Info,
        /// <summary>
        /// Yellow text
        /// </summary>
        Warning,
        /// <summary>
        /// Red text
        /// </summary>
        Error,
        /// <summary>
        /// Green text
        /// </summary>
        Success,
        /// <summary>
        /// White text
        /// </summary>
        Debug,
        /// <summary>
        /// Pink-ish text
        /// </summary>
        Important,
        /// <summary>
        /// Black text with yellow background
        /// </summary>
        Performance,
    }

    public class LogInfo
    {
        public string Text;
        public ConsoleColor ConsoleColor;
        public Vector4 Color;
    }

    public class LogDetails
    {
        public List<LogInfo> Log = new List<LogInfo>();

        public LogLevel Level;

        public object Tag;

        public void Add(string text, ConsoleColor consoleColor, Vector4 color)
        {
            Log.Add(new LogInfo() { Text = text, ConsoleColor = consoleColor, Color = color});
        }
    }

    public static class Utils
    {
        public const int MAX_LOGS = 50;

        public static string BasePath { get; private set; }

        public static event Action<LogDetails> OnLog;

        public static List<LogDetails> Logs = new List<LogDetails>();

        private static Assembly easy2dAssembly;

        public static bool WriteToConsole = false;

        public static HashSet<LogLevel> IgnoredLogLevels = new HashSet<LogLevel>();

        static Utils()
        {
            BasePath = AppContext.BaseDirectory;

            easy2dAssembly = Assembly.GetExecutingAssembly();

            foreach (var item in easy2dAssembly.GetManifestResourceNames())
            {
                Log($"Internal Resource: {item}", LogLevel.Debug);
            }

            OnLog += (s) =>
            {
                if (WriteToConsole)
                {
                    foreach (var item in s.Log)
                    {
                        writeColor(item.Text, item.ConsoleColor);
                    }
                }

                if (Logs.Count > MAX_LOGS)
                    Logs.RemoveAt(0);

                Logs.Add(s);
            };

            Log($"BasePath: {BasePath}", LogLevel.Debug);
        }

        internal static Stream GetInternalResource(string name)
        {
            string fullname = $"Easy2D.{name}";
            Stream stream = easy2dAssembly.GetManifestResourceStream(fullname);

            return stream;
        }

        public static Stream GetResource(string name)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            string fullname = $"{callingAssembly.GetName().Name}.{name}";
            Stream stream = callingAssembly.GetManifestResourceStream(fullname);

            return stream;
        }

        public static void Log(string message, LogLevel level)
        {
            if (IgnoredLogLevels.Contains(level))
                return;

            var callingMethod = new StackTrace().GetFrame(1).GetMethod();

            string method = $"{callingMethod.DeclaringType.Name}.{callingMethod.Name}";

            LogDetails details = new LogDetails();

            details.Add("[", ConsoleColor.DarkGray, (Vector4)Color4.Gray);
            details.Add($"{DateTime.Now.ToString("HH:mm:ss")} ", ConsoleColor.Gray, (Vector4)Color4.LightGray);
            details.Add($"{level} ", logLevelToConsoleColor(level), (Vector4)logLevelToColor(level));

            details.Add(method, ConsoleColor.Blue, (Vector4)Color4.BlueViolet);

            details.Add($"] ", ConsoleColor.DarkGray, (Vector4)Color4.Gray);

            details.Add($"{message}\n", ConsoleColor.White, (Vector4)Color4.White);

            details.Level = level;

            OnLog?.Invoke(details);
        }

        private static void writeColor(string text, ConsoleColor color)
        {
            //This throws platform not supported exception on android and ios
            try
            {
                Console.ForegroundColor = color;
            }
            catch { }

            Console.Write(text);
        }

        private static ConsoleColor logLevelToConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info:
                    return ConsoleColor.Cyan;
                case LogLevel.Warning:
                    return ConsoleColor.Yellow;
                case LogLevel.Error:
                    return ConsoleColor.Red;
                case LogLevel.Success:
                    return ConsoleColor.Green;
                case LogLevel.Debug:
                    return ConsoleColor.White;
                case LogLevel.Important:
                    return ConsoleColor.Magenta;
                default:
                    return ConsoleColor.DarkGray;
            }
        }

        private static Color4 logLevelToColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info:
                    return Color4.Cyan;
                case LogLevel.Warning:
                    return Color4.Yellow;
                case LogLevel.Error:
                    return Color4.Red;
                case LogLevel.Success:
                    return Color4.LawnGreen;
                case LogLevel.Debug:
                    return Color4.White;
                case LogLevel.Important:
                    return Color4.Magenta;
                default:
                    return Color4.DarkGray;
            }
        }

        //Credits: CPP guy on discord "Anon"
        public static (int seconds, int milliseconds, int microseconds, int nanoseconds) ConvertTime(double second)
        {
            int seconds = (int)(second);
            int milliseconds = (int)(second * 1000) - seconds * 1000;
            int microseconds = (int)(second * 1000000) - (seconds * 1000000 + milliseconds * 1000);
            int nanoseconds = (int)(second * 1000000000) - (seconds * 1000000000 + milliseconds * 1000000 + microseconds * 1000);

            return (seconds, milliseconds, microseconds, nanoseconds);
        }

        /// <summary>
        /// Convert number to kilo, million, billion
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ToKMB(this double number)
        {
            number = Math.Abs(number);

            if (number > 999999999)
                return number.ToString("0,,,.###B", CultureInfo.InvariantCulture);
            else if (number > 999999)
                return number.ToString("0,,.##M", CultureInfo.InvariantCulture);
            else if (number > 999)
                return number.ToString("0,.#K", CultureInfo.InvariantCulture);
            else
                return number.ToString(CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, Stopwatch> benchmarks = new Dictionary<string, Stopwatch>();
        public static void BeginProfiling(string name) => benchmarks.Add(name, Stopwatch.StartNew());

        public static double EndProfiling(string name)
        {
            var time = ((double)benchmarks[name].ElapsedTicks / Stopwatch.Frequency) * 1000.0;
            benchmarks.Remove(name);

            Utils.Log($"{name} took {time} milliseconds", LogLevel.Performance);

            return time;
        }

        public static double Benchmark(Action a)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            a?.Invoke();
            sw.Stop();
            return ((double)sw.ElapsedTicks / Stopwatch.Frequency) * 1000.0;
        }

        public static void Benchmark(Action a, string name)
        {
            var time = Benchmark(a);
            Utils.Log($"{name} took {time} milliseconds", LogLevel.Performance);
        }

        public static string ComputeSHA256Hash(byte[] buffer, int offset, int length)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(buffer, offset, length);

                return Convert.ToBase64String(hash);
            }
        }

        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
