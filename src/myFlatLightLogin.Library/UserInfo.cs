using Csla;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myFlatLightLogin.Library
{
    [Serializable]
    public class UserInfo : ReadOnlyBase<UserInfo>
    {
        #region Properties

        /// <summary>
        /// Local database ID 
        /// </summary>
        public static readonly PropertyInfo<int> IdProperty = RegisterProperty<int>(c => c.Id);
        public int Id
        {
            get { return GetProperty(IdProperty); }
            set { LoadProperty(IdProperty, value); }
        }

        public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(c => c.Name);
        public string Name
        {
            get { return GetProperty(NameProperty); }
            set { LoadProperty(NameProperty, value); }
        }

        public static readonly PropertyInfo<string> LastNameProperty = RegisterProperty<string>(c => c.LastName);
        public string LastName
        {
            get { return GetProperty(LastNameProperty); }
            set { LoadProperty(LastNameProperty, value); }
        }

        public static readonly PropertyInfo<string> UserNameProperty = RegisterProperty<string>(c => c.UserName);
        public string UserName
        {
            get { return GetProperty(UserNameProperty); }
            set { LoadProperty(UserNameProperty, value); }
        }

        public static readonly PropertyInfo<string> EmailProperty = RegisterProperty<string>(c => c.Email);
        public string Email
        {
            get { return GetProperty(EmailProperty); }
            set { LoadProperty(EmailProperty, value); }
        }

        public static readonly PropertyInfo<string> PasswordProperty = RegisterProperty<string>(c => c.Password);
        public string Password
        {
            get { return GetProperty(PasswordProperty); }
            set { LoadProperty(PasswordProperty, value); }
        }

        //public static readonly PropertyInfo<string> PasswordProperty = RegisterProperty<string>(c => c.Password);
        //public UserRole Password
        //{
        //    get { return GetProperty(PasswordProperty); }
        //    set { LoadProperty(PasswordProperty, value); }
        //}

        #endregion
    }
}
