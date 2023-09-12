﻿using myFlatLightLogin.Core.Services;

namespace myFlatLightLogin.Core.MVVM
{
    public class ViewModelBase : BindableBase
    {
        //public RelayCommand ShowLoginView { get; set; }
        //public RelayCommand ShowRegisterUserView { get; set; }


        //private object _currentView;
        //public object CurrentView
        //{
        //    get { return _currentView; }
        //    set { SetProperty(ref _currentView, value); }
        //}

        //public ViewModelBase()
        //{
        //ShowLoginView = new RelayCommand(o => CurrentView = LoginVM, o => true);
        //ShowRegisterUserView = new RelayCommand(o => CurrentView = RegisterUserVM);
        //}

        private INavigationService _navigation;
        public INavigationService Navigation
        {
            get => _navigation;
            set { SetProperty(ref _navigation, value); }
        }
    }
}