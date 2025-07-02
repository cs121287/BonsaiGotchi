using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Text;
using BonsaiGotchiGame.Services;
using BonsaiGotchiGame.ViewModels;

namespace BonsaiGotchiGame
{
    public partial class App : Application
    {
        private SaveLoadService? _saveLoadService;
        
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
                
                // Create application data directory if it doesn't exist
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BonsaiGotchiGame");
                    
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                    LogStartup($"Created app data directory: {appDataPath}");
                }
                
                // Create and show the main window on the UI thread with a timeout
                Dispatcher.Invoke(() => {
                    try
                    {
                        LogStartup("Creating MainWindow instance");
                        MainWindow = new MainWindow();
                        LogStartup("MainWindow instance created");
                        
                        LogStartup("Showing MainWindow");
                        MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        MainWindow.Visibility = Visibility.Visible;
                        MainWindow.Show();
                        MainWindow.Activate();
                        LogStartup("MainWindow displayed and activated");
                    }
                    catch (Exception ex)
                    {
                        LogStartup($"Error creating/showing MainWindow: {ex.Message}");
                        LogStartup($"Stack trace: {ex.StackTrace}");
                        MessageBox.Show($"Error creating application window: {ex.Message}\n\n{ex.StackTrace}",
                            "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }, DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                LogStartup($"Critical error in OnStartup: {ex.Message}");
                LogStartup($"Stack trace: {ex.StackTrace}");
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
                
                File.AppendAllText(logPath, logEntry);
                Debug.WriteLine($"BonsaiGotchi: {message}");
            }
            catch
            {
                // If logging fails, there's not much we can do
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
                        // Save the bonsai's state when the application exits
                        _saveLoadService.SaveBonsaiAsync(viewModel.Bonsai).Wait();
                        LogStartup("Final save completed");
                    }
                    
                    // Make sure the window properly disposes its resources
                    (mainWindow as IDisposable)?.Dispose();
                    LogStartup("MainWindow disposed");
                }
                
                // Release service references
                _saveLoadService = null;
                
                // Force garbage collection before exit
                GC.Collect();
                GC.WaitForPendingFinalizers();
                LogStartup("Application exit complete");
            }
            catch (Exception ex)
            {
                LogStartup($"Error during exit: {ex.Message}");
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
            catch
            {
                // If we can't even show an error message, there's not much we can do
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
            catch
            {
                // If we can't even show an error message, there's not much we can do
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
                    
                string logEntry = $"[{DateTime.Now}] {ex.GetType()}: {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText(logPath, logEntry);
                Debug.WriteLine($"BonsaiGotchi ERROR: {ex.Message}");
            }
            catch
            {
                // If logging fails, there's not much we can do
            }
        }
    }
}