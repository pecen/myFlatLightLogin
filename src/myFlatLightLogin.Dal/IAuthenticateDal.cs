using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myFlatLightLogin.Dal
{
    public interface IAuthenticateDal
    {
        bool Authenticate(string username, string password);
        bool RegisterUser(string username, string password);
    }
}
