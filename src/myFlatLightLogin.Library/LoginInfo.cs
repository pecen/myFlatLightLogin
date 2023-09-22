using Csla;
using myFlatLightLogin.Dal;
using System;

namespace myFlatLightLogin.Library
{
    [Serializable]
    public class LoginInfo : ReadOnlyBase<LoginInfo>
    {
        #region Properties

        public static readonly PropertyInfo<string> UserIdProperty = RegisterProperty<string>(c => c.UserId);
        public string UserId
        {
            get { return GetProperty(UserIdProperty); }
            set { LoadProperty(UserIdProperty, value); }
        }

        public static readonly PropertyInfo<string> PasswordProperty = RegisterProperty<string>(c => c.Password);
        public string Password
        {
            get { return GetProperty(PasswordProperty); }
            set { LoadProperty(PasswordProperty, value); }
        }

        #endregion

        #region Factory Methods

        public static LoginInfo Login()
        {
            return DataPortal.Fetch<LoginInfo>();
        }

        #endregion

        #region Data Access

        [Insert]
        private void Fetch()
        {
            using (var dalManager = DalFactory.GetManager())
            {
                //var dal = dalManager.GetProvider<IAuthenticateDal>();
                //var data = dal.Fetch();

                //DalManagerType = data.DalManagerType;
                //Password = data.BaseUri;
                //ClientSecret = data.ClientSecret;
                //DbInUse = data.DbInUse;
            }
        }

        #endregion
    }
}
