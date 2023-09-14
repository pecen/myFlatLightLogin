using myFlatLightLogin.Core.MVVM;
using System;

namespace myFlatLightLogin.Core.Services
{
    public interface INavigationService
    {
        ViewModelBase CurrentView { get; }

        void NavigateTo<T>(Action<string> callback) where T : ViewModelBase;
    }
}
