using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Threading;

namespace BonsaiGotchiGame
{
    public class Program
    {
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
                
                // Create AppDomain exception handler
                AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                    Exception? ex = e.ExceptionObject as Exception;
                    LogStartup($"UNHANDLED EXCEPTION: {ex?.GetType()}: {ex?.Message}");
                    LogStartup($"Stack trace: {ex?.StackTrace}");
                };
                
                // Create application data directory if it doesn't exist
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BonsaiGotchiGame");
                    
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                    LogStartup($"Created app data directory: {appDataPath}");
                }
                
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
                LogStartup($"Stack trace: {ex.StackTrace}");
                
                try
                {
                    MessageBox.Show($"A critical error prevented the application from starting:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                                   "Fatal Error", 
                                   MessageBoxButton.OK, 
                                   MessageBoxImage.Error);
                }
                catch
                {
                    // If even MessageBox fails, try to show a console message
                    Console.WriteLine($"FATAL ERROR: {ex.Message}");
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
                    
                File.AppendAllText(logPath, logEntry);
                Debug.WriteLine($"Program: {message}");
            }
            catch
            {
                // If logging fails, there's not much we can do
            }
        }
    }
}