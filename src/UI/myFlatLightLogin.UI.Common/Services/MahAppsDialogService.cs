using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Common.Services
{
    /// <summary>
    /// Concrete MahApps dialog service that routes calls to Application.Current.MainWindow.
    /// Ensures the dialog call is marshalled to the UI thread when needed.
    /// </summary>
    public class MahAppsDialogService : IDialogService
    {
        public Task<MessageDialogResult> ShowMessageAsync(string title, string message,
            MessageDialogStyle style = MessageDialogStyle.Affirmative,
            MetroDialogSettings? settings = null)
        {
            var window = (MetroWindow)Application.Current.MainWindow;
            settings ??= new MetroDialogSettings { AnimateShow = true, AnimateHide = true };

            if (window.Dispatcher.CheckAccess())
            {
                return window.ShowMessageAsync(title, message, style, settings);
            }

            // Marshal to UI thread and unwrap the Task<MessageDialogResult>
            return window.Dispatcher.InvokeAsync(() => window.ShowMessageAsync(title, message, style, settings))
                         .Task.Unwrap();
        }
    }
}
