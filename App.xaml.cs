using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using BonsaiGotchiGame.Services;
using BonsaiGotchiGame.ViewModels;

namespace BonsaiGotchiGame
{
    public partial class App : Application
    {
        private SaveLoadService? _saveLoadService;
        private static readonly object _logLock = new object();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set up global exception handling first
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            try
            {
                LogStartup("Application startup initiated");

                // Initialize services
                _saveLoadService = new SaveLoadService();
                LogStartup("SaveLoadService initialized");

                // Directory creation is now handled in Program.cs, but we verify it exists
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BonsaiGotchiGame");

                if (!Directory.Exists(appDataPath))
                {
                    try
                    {
                        Directory.CreateDirectory(appDataPath);
                        LogStartup($"Created app data directory: {appDataPath}");
                    }
                    catch (Exception ex)
                    {
                        LogStartup($"Failed to create app data directory: {ex.Message}");
                        throw new InvalidOperationException($"Could not create application data directory: {appDataPath}", ex);
                    }
                }

                // FIXED: Changed how the MainWindow is created and initialized
                // Create the window first without showing it
                LogStartup("Creating MainWindow instance");
                MainWindow = new MainWindow();
                LogStartup("MainWindow instance created");

                // Use Loaded priority instead of Background to avoid race conditions
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    try
                    {
                        LogStartup("Showing MainWindow");
                        if (MainWindow != null)
                        {
                            MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            MainWindow.Show();
                            MainWindow.Activate();
                            LogStartup("MainWindow displayed and activated");
                        }
                        else
                        {
                            LogStartup("MainWindow is null, cannot display");
                            throw new InvalidOperationException("MainWindow was not properly initialized");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogStartup($"Error showing MainWindow: {ex.Message}");
                        if (ex.StackTrace != null)
                        {
                            LogStartup($"Stack trace: {ex.StackTrace}");
                        }
                        MessageBox.Show($"Error displaying application window: {ex.Message}",
                            "Display Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }));
            }
            catch (Exception ex)
            {
                LogStartup($"Critical error in OnStartup: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    LogStartup($"Stack trace: {ex.StackTrace}");
                }
                MessageBox.Show($"Error during application startup: {ex.Message}\n\n{ex.StackTrace}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogStartup(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"[{timestamp}] {message}\n";

                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BonsaiGotchiGame",
                    "startup_log.txt");

                // Use lock to prevent concurrent access issues
                lock (_logLock)
                {
                    File.AppendAllText(logPath, logEntry);
                }

                Debug.WriteLine($"BonsaiGotchi: {message}");
            }
            catch (Exception ex)
            {
                // Log to debug output if file logging fails
                Debug.WriteLine($"BonsaiGotchi logging failed: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                LogStartup("Application exiting");

                // Perform any cleanup or final save operations
                if (MainWindow is MainWindow mainWindow && _saveLoadService != null)
                {
                    if (mainWindow.DataContext is ViewModels.MainViewModel viewModel && viewModel.Bonsai != null)
                    {
                        // FIXED: Use proper async handling instead of .Wait() to avoid deadlocks
                        try
                        {
                            // Use Task.Run to avoid UI thread blocking
                            Task.Run(async () =>
                            {
                                await _saveLoadService.SaveBonsaiAsync(viewModel.Bonsai).ConfigureAwait(false);
                            }).Wait(TimeSpan.FromSeconds(5)); // Give it 5 seconds max to complete

                            LogStartup("Final save completed");
                        }
                        catch (Exception saveEx)
                        {
                            LogStartup($"Error during final save: {saveEx.Message}");
                        }
                    }

                    // FIXED: Proper disposal check and handling
                    if (mainWindow is IDisposable disposable)
                    {
                        disposable.Dispose();
                        LogStartup("MainWindow disposed");
                    }
                }

                // Release service references
                _saveLoadService = null;

                // REMOVED: Forced garbage collection as it's not recommended
                LogStartup("Application exit complete");
            }
            catch (Exception ex)
            {
                LogStartup($"Error during exit: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    LogStartup($"Stack trace: {ex.StackTrace}");
                }
                MessageBox.Show($"Error saving data on exit: {ex.Message}",
                    "Exit Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            base.OnExit(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogError(e.ExceptionObject as Exception);

            try
            {
                MessageBox.Show($"An unexpected error occurred: {(e.ExceptionObject as Exception)?.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Log the error instead of silently ignoring
                LogError(ex);
                Debug.WriteLine($"Failed to show error message: {ex.Message}");
            }
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError(e.Exception);

            try
            {
                MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Log the error instead of silently ignoring
                LogError(ex);
                Debug.WriteLine($"Failed to show error message: {ex.Message}");
            }

            e.Handled = true;
        }

        private void LogError(Exception? ex)
        {
            if (ex == null)
                return;

            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BonsaiGotchiGame",
                    "error_log.txt");

                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {ex.GetType()}: {ex.Message}\n{ex.StackTrace}\n\n";

                // Use lock to prevent concurrent access issues
                lock (_logLock)
                {
                    File.AppendAllText(logPath, logEntry);
                }

                Debug.WriteLine($"BonsaiGotchi ERROR: {ex.Message}");
            }
            catch (Exception logEx)
            {
                // Log to debug output if file logging fails
                Debug.WriteLine($"BonsaiGotchi error logging failed: {logEx.Message}");
            }
        }
    }
}