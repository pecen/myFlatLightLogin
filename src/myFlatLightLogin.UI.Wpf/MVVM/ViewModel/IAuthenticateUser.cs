﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public interface IAuthenticateUser
    {
        bool IsAuthenticated { get; }
        bool PwdIsEmpty { get; set; }
    }
}