using myFlatLightLogin.Dal.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myFlatLightLogin.Dal
{
    public interface IUserDal
    {
        UserDto Fetch(int id);
        //List<UserDto> Fetch();
        bool Insert(UserDto user);
        bool Update(UserDto user);
        bool Delete(int id);
    }
}
