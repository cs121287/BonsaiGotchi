using System;
using System.Windows.Forms;
using System.Threading;

namespace BonsaiGotchi
{
    /// <summary>
    /// Main entry point for the BonsaiGotchi application
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Set up exception handling for unhandled exceptions
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // Apply application configuration
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            try
            {
                // Create resource directories if they don't exist
                EnsureResourceDirectoriesExist();
                
                // Start the application
                Application.Run(new BonsaiGotchiForm());
                
                // Clean up resources when application exits
                Properties.Resources.CleanupResources();
            }
            catch (Exception ex)
            {
                HandleFatalException(ex);
            }
        }

        /// <summary>
        /// Ensures that required resource directories exist
        /// </summary>
        private static void EnsureResourceDirectoriesExist()
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                System.IO.Directory.CreateDirectory(Path.Combine(baseDirectory, "Resources"));
                System.IO.Directory.CreateDirectory(Path.Combine(baseDirectory, "SaveData"));
            }
            catch (Exception)
            {
                // If we can't create directories, we'll just continue and fail later if needed
            }
        }

        /// <summary>
        /// Handles UI thread exceptions
        /// </summary>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleFatalException(e.Exception);
        }

        /// <summary>
        /// Handles non-UI thread exceptions
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleFatalException(ex);
            }
            else
            {
                HandleFatalException(new Exception("An unknown error occurred."));
            }
        }

        /// <summary>
        /// Handles fatal exceptions by displaying error message and logging
        /// </summary>
        private static void HandleFatalException(Exception ex)
        {
            try
            {
                // Log the exception
                string logPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "BonsaiGotchi_ErrorLog.txt");
                
                string errorMessage = $"[{DateTime.Now}] Error: {ex.Message}\r\nStack Trace: {ex.StackTrace}";
                
                File.AppendAllText(logPath, errorMessage + "\r\n\r\n");
                
                // Show error message
                MessageBox.Show(
                    $"An error occurred in BonsaiGotchi:\n\n{ex.Message}\n\nThe error has been logged to:\n{logPath}",
                    "BonsaiGotchi Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // If logging fails, just show a simple error message
                MessageBox.Show(
                    $"A critical error occurred in BonsaiGotchi:\n\n{ex.Message}",
                    "BonsaiGotchi Critical Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}