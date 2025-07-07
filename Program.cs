using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BonsaiGotchiGame
{
    public class Program
    {
        private static readonly object _logLock = new object();

        [STAThread]
        public static void Main()
        {
            // Enable Windows Forms integration if needed
            //System.Windows.Forms.Application.EnableVisualStyles();
            //System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Log startup
                LogStartup("Program entry point reached");

                // Create application data directory if it doesn't exist (consolidated here)
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BonsaiGotchiGame");

                try
                {
                    if (!Directory.Exists(appDataPath))
                    {
                        Directory.CreateDirectory(appDataPath);
                        LogStartup($"Created app data directory: {appDataPath}");
                    }
                }
                catch (Exception ex)
                {
                    LogStartup($"Failed to create app data directory: {ex.Message}");
                    throw new InvalidOperationException($"Could not create application data directory: {appDataPath}", ex);
                }

                // Create AppDomain exception handler
                AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                    Exception? ex = e.ExceptionObject as Exception;
                    LogStartup($"UNHANDLED EXCEPTION: {ex?.GetType()}: {ex?.Message}");
                    if (ex?.StackTrace != null)
                    {
                        LogStartup($"Stack trace: {ex.StackTrace}");
                    }
                };

                // Launch the application
                LogStartup("Creating application instance");
                var app = new App();
                LogStartup("Running application");
                app.Run();
                LogStartup("Application exited normally");
            }
            catch (Exception ex)
            {
                LogStartup($"CRITICAL ERROR: {ex.GetType()}: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    LogStartup($"Stack trace: {ex.StackTrace}");
                }

                try
                {
                    MessageBox.Show($"A critical error prevented the application from starting:\n\n{ex.Message}\n\n{ex.StackTrace}",
                                   "Fatal Error",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
                catch (Exception msgBoxEx)
                {
                    // If even MessageBox fails, try to show a console message
                    Console.WriteLine($"FATAL ERROR: {ex.Message}");
                    Console.WriteLine($"Additional error showing message box: {msgBoxEx.Message}");
                }
            }
        }

        private static void LogStartup(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"[{timestamp}] {message}\n";

                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BonsaiGotchiGame",
                    "program_log.txt");

                // Use lock to prevent concurrent access issues
                lock (_logLock)
                {
                    File.AppendAllText(logPath, logEntry);
                }

                Debug.WriteLine($"Program: {message}");
            }
            catch (Exception ex)
            {
                // Log to debug output if file logging fails
                Debug.WriteLine($"Program logging failed: {ex.Message}");
            }
        }
    }
}