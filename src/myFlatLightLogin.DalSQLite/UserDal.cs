using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.DalSQLite.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myFlatLightLogin.DalSQLite
{
    public class UserDal : IUserDal
    {
        public bool Delete(int id)
        {
            throw new NotImplementedException();
        }

        public UserDto Fetch(int id)
        {
            List<UserDto> items = DbCore.Fetch<UserDto>();

            return items.FirstOrDefault(i => i.Id == id);
        }

        public bool Insert(UserDto user)
        {
            return DbCore.Insert(new User
            {
                Name = user.Name,
                Lastname = user.Lastname,
                Username = user.Username,
                Password = user.Password
            });
        }

        public bool Update(UserDto user)
        {
            throw new NotImplementedException();
        }
    }
}
