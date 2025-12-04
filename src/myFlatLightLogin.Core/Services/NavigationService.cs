using myFlatLightLogin.UI.Common.MVVM;
using System;

namespace myFlatLightLogin.Core.Services
{
    public class NavigationService : BindableBase, INavigationService
    {
        private ViewModelBase _currentView;
        private readonly Func<Type, ViewModelBase> _viewModelFactory;

        public ViewModelBase CurrentView
        {
            get => _currentView;
            private set { SetProperty(ref _currentView, value); }
        }

        public NavigationService(Func<Type, ViewModelBase> viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
        {
            ViewModelBase viewModel = _viewModelFactory.Invoke(typeof(TViewModel));
            if (viewModel == CurrentView)
            {
                CurrentView = null;
            }
            else
            {
                CurrentView = viewModel;
            }
        }
    }
}
