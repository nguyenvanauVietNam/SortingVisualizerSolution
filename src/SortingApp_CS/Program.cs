using System;
using System.Windows.Forms;

namespace SortingApp_CS
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            ShowUnhandledError(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowUnhandledError(e.ExceptionObject as Exception);
        }

        private static void ShowUnhandledError(Exception exception)
        {
            string message = "The application encountered an unexpected error.";
            if (exception != null && !string.IsNullOrWhiteSpace(exception.Message))
            {
                message += Environment.NewLine + Environment.NewLine + exception.Message;
            }

            try
            {
                MessageBox.Show(
                    message,
                    "Sorting Visualizer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }
        }
    }
}
