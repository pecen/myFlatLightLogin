using Csla;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Threading.Tasks;

namespace myFlatLightLogin.Library
{
    /// <summary>
    /// Read-only business object for displaying Role information.
    /// Used for read-only scenarios like displaying role details in lists.
    /// </summary>
    [Serializable]
    public class RoleInfo : ReadOnlyBase<RoleInfo>
    {
        #region Properties

        public static readonly PropertyInfo<int> IdProperty = RegisterProperty<int>(c => c.Id);
        /// <summary>
        /// Role ID (1 = User, 2 = Admin).
        /// </summary>
        public int Id
        {
            get => GetProperty(IdProperty);
            private set => LoadProperty(IdProperty, value);
        }

        public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(c => c.Name);
        /// <summary>
        /// Role name (e.g., "User", "Admin").
        /// </summary>
        public string Name
        {
            get => GetProperty(NameProperty);
            private set => LoadProperty(NameProperty, value);
        }

        public static readonly PropertyInfo<string> DescriptionProperty = RegisterProperty<string>(c => c.Description);
        /// <summary>
        /// Optional description of the role.
        /// </summary>
        public string Description
        {
            get => GetProperty(DescriptionProperty);
            private set => LoadProperty(DescriptionProperty, value);
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Gets a read-only role by ID.
        /// </summary>
        public static async Task<RoleInfo> GetRoleAsync(int id)
        {
            return await DataPortal.FetchAsync<RoleInfo>(id);
        }

        /// <summary>
        /// Gets a read-only role by name.
        /// </summary>
        public static async Task<RoleInfo> GetRoleByNameAsync(string name)
        {
            return await DataPortal.FetchAsync<RoleInfo>(new RoleFetchCriteria { Name = name });
        }

        /// <summary>
        /// Creates a RoleInfo from a RoleDto.
        /// </summary>
        internal static RoleInfo FromDto(RoleDto dto)
        {
            var roleInfo = new RoleInfo();
            roleInfo.LoadFromDto(dto);
            return roleInfo;
        }

        #endregion

        #region Data Access

        [Serializable]
        private class RoleFetchCriteria
        {
            public string Name { get; set; }
        }

        [Fetch]
        private void Fetch(int id)
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IRoleDal>();
                var data = dal.Fetch(id);

                if (data == null)
                    throw new Exception($"Role with ID {id} not found");

                LoadFromDto(data);
            }
        }

        [Fetch]
        private void Fetch(RoleFetchCriteria criteria)
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IRoleDal>();
                var data = dal.Fetch(criteria.Name);

                if (data == null)
                    throw new Exception($"Role '{criteria.Name}' not found");

                LoadFromDto(data);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Loads properties from a RoleDto.
        /// </summary>
        private void LoadFromDto(RoleDto dto)
        {
            LoadProperty(IdProperty, dto.Id);
            LoadProperty(NameProperty, dto.Name ?? string.Empty);
            LoadProperty(DescriptionProperty, dto.Description ?? string.Empty);
        }

        #endregion
    }
}
