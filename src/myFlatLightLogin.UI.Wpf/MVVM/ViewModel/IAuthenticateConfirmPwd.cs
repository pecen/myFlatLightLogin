using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public interface IAuthenticateConfirmPwd : IAuthenticatePassword
    {
        bool ConfirmPwdIsEmpty { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
