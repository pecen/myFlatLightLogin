using Csla;
using Csla.Core;
using Csla.Rules;
using Csla.Rules.CommonRules;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Threading.Tasks;

namespace myFlatLightLogin.Library
{
    /// <summary>
    /// Editable business object for Role management.
    /// Handles role creation, updates, and business rules.
    /// </summary>
    [Serializable]
    public class RoleEdit : BusinessBase<RoleEdit>
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
            set => SetProperty(NameProperty, value);
        }

        public static readonly PropertyInfo<string> DescriptionProperty = RegisterProperty<string>(c => c.Description);
        /// <summary>
        /// Optional description of the role.
        /// </summary>
        public string Description
        {
            get => GetProperty(DescriptionProperty);
            set => SetProperty(DescriptionProperty, value);
        }

        #endregion

        #region Business Rules

        protected override void AddBusinessRules()
        {
            base.AddBusinessRules();

            // Name is required
            BusinessRules.AddRule(new Required(NameProperty));

            // Name must be at least 2 characters
            BusinessRules.AddRule(new MinLength(NameProperty, 2));

            // Name must be unique (this would require a custom rule checking against database)
            // BusinessRules.AddRule(new UniqueRoleNameRule(NameProperty));
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a new role.
        /// </summary>
        public static async Task<RoleEdit> NewRoleAsync()
        {
            return await DataPortal.CreateAsync<RoleEdit>();
        }

        /// <summary>
        /// Gets an existing role by ID.
        /// </summary>
        public static async Task<RoleEdit> GetRoleAsync(int id)
        {
            return await DataPortal.FetchAsync<RoleEdit>(id);
        }

        /// <summary>
        /// Gets an existing role by name.
        /// </summary>
        public static async Task<RoleEdit> GetRoleByNameAsync(string name)
        {
            return await DataPortal.FetchAsync<RoleEdit>(new RoleFetchCriteria { Name = name });
        }

        /// <summary>
        /// Deletes a role by ID.
        /// </summary>
        public static async Task DeleteRoleAsync(int id)
        {
            await DataPortal.DeleteAsync<RoleEdit>(id);
        }

        #endregion

        #region Data Access

        [Serializable]
        private class RoleFetchCriteria
        {
            public string? Name { get; set; }
        }

        [Create]
        private void Create()
        {
            // Set defaults for new role
            Id = 0;
            Name = string.Empty;
            Description = string.Empty;
            BusinessRules.CheckRules();
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

        [Insert]
        private void Insert()
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IRoleDal>();
                var dto = ToDto();

                bool success = dal.Insert(dto);

                if (!success)
                    throw new Exception("Failed to insert role");
            }
        }

        [Update]
        private void Update()
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IRoleDal>();
                var dto = ToDto();

                bool success = dal.Update(dto);

                if (!success)
                    throw new Exception("Failed to update role");
            }
        }

        [DeleteSelf]
        private void DeleteSelf()
        {
            Delete(Id);
        }

        [Delete]
        private void Delete(int id)
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IRoleDal>();
                bool success = dal.Delete(id);

                if (!success)
                    throw new Exception("Failed to delete role");
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

        /// <summary>
        /// Converts the business object to a RoleDto.
        /// </summary>
        private RoleDto ToDto()
        {
            return new RoleDto
            {
                Id = Id,
                Name = Name,
                Description = Description
            };
        }

        #endregion
    }
}
