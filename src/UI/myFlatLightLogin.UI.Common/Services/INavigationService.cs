using myFlatLightLogin.UI.Common.MVVM;

namespace myFlatLightLogin.UI.Common.Services
{
    public interface INavigationService
    {
        ViewModelBase CurrentView { get; }

        void NavigateTo<T>() where T : ViewModelBase;
    }
}
