using Csla;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace myFlatLightLogin.Library
{
    /// <summary>
    /// Read-only list of RoleInfo objects.
    /// Used for displaying all roles in the system.
    /// </summary>
    [Serializable]
    public class RoleList : ReadOnlyListBase<RoleList, RoleInfo>
    {
        #region Factory Methods

        /// <summary>
        /// Gets a list of all roles.
        /// </summary>
        public static async Task<RoleList> GetRolesAsync()
        {
            return await DataPortal.FetchAsync<RoleList>();
        }

        #endregion

        #region Data Access

        [Fetch]
        private void Fetch()
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IRoleDal>();
                var roles = dal.Fetch();

                LoadRoles(roles);
            }
        }

        /// <summary>
        /// Loads roles from a list of DTOs.
        /// </summary>
        private void LoadRoles(List<RoleDto> roles)
        {
            IsReadOnly = false;

            foreach (var dto in roles)
            {
                Add(RoleInfo.FromDto(dto));
            }

            IsReadOnly = true;
        }

        #endregion
    }
}
