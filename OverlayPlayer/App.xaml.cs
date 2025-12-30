using System;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace OverlayPlayer
{
    public partial class App : System.Windows.Application
    {
        private static DateTime _lastErrorShown = DateTime.MinValue;
        private static string _lastErrorMessage = string.Empty;
        private const int ErrorCooldownSeconds = 5; // Don't show same error more than once per 5 seconds

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Handle unhandled exceptions on UI thread
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Handle unhandled exceptions on background threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Handle unobserved task exceptions
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the exception
            string errorMessage = e.Exception.Message;
            string exceptionType = e.Exception.GetType().Name;
            string stackTrace = e.Exception.StackTrace ?? "No stack trace available";
            
            System.Diagnostics.Debug.WriteLine($"Unhandled UI Exception ({exceptionType}): {errorMessage}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {stackTrace}");

            // Log to file with more details
            try
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "OverlayPlayer",
                    "error.log"
                );
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.AppendAllText(logPath, 
                    $"[{DateTime.Now}] UI Exception ({exceptionType}): {errorMessage}\n" +
                    $"Stack Trace:\n{stackTrace}\n" +
                    $"Inner Exception: {e.Exception.InnerException?.Message ?? "None"}\n\n");
            }
            catch { }

            // Only show MessageBox if it's a different error or enough time has passed
            bool shouldShow = (DateTime.Now - _lastErrorShown).TotalSeconds > ErrorCooldownSeconds 
                           || _lastErrorMessage != errorMessage;

            if (shouldShow)
            {
                _lastErrorShown = DateTime.Now;
                _lastErrorMessage = errorMessage;

                // Show error message only once per error type
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{errorMessage}\n\n" +
                    "The application will continue, but some features may not work correctly.\n\n" +
                    "Check error.log for details.",
                    "Application Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            // Mark as handled to prevent app crash
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception? ex = e.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"Unhandled Domain Exception: {ex?.Message ?? "Unknown"}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex?.StackTrace ?? "N/A"}");
            
            // Log to file if possible
            try
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "OverlayPlayer",
                    "error.log"
                );
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.AppendAllText(logPath, 
                    $"[{DateTime.Now}] Unhandled Exception: {ex?.Message}\n{ex?.StackTrace}\n\n");
            }
            catch { }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Unobserved Task Exception: {e.Exception}");
            e.SetObserved(); // Mark as observed to prevent app crash
        }
    }
}
