using MahApps.Metro.Controls.Dialogs;
using System.Threading.Tasks;

namespace myFlatLightLogin.UI.Common.Services
{
    /// <summary>
    /// Abstraction for showing MahApps dialogs from viewmodels (keeps viewmodels UI-agnostic).
    /// </summary>
    public interface IDialogService
    {
        Task<MessageDialogResult> ShowMessageAsync(string title, string message,
            MessageDialogStyle style = MessageDialogStyle.Affirmative,
            MetroDialogSettings? settings = null);
    }
}
