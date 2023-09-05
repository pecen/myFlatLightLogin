using myFlatLightLogin.UI.Wpf.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        // Enable if EventTrigger is to be used
        //public RelayCommand<object> SetPwdStatusCommand { get; set; }

        private bool _pwdIsEmpty = true;
        public bool PwdIsEmpty
        {
            get { return _pwdIsEmpty; }
            set { SetProperty(ref _pwdIsEmpty, value); }
        }

        public LoginViewModel()
        {
            // Enable if EventTrigger is to be used
            //SetPwdStatusCommand = new RelayCommand<object>(SetStatus);
        }

        // Used if EventTrigger is used
        //private void SetStatus(object pwdBox)
        //{
        //    if (pwdBox is PasswordBox pwd)
        //    {
        //        if (pwd.SecurePassword.Length > 0)
        //        {
        //            PwdIsEmpty = false;
        //        }
        //        else
        //        {
        //            PwdIsEmpty = true;
        //        }
        //    }
        //}
    }
}
