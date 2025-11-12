using myFlatLightLogin.Dal.Dto;
using System.Collections.Generic;

namespace myFlatLightLogin.Dal
{
    /// <summary>
    /// Interface for Role data access operations.
    /// </summary>
    public interface IRoleDal
    {
        /// <summary>
        /// Gets a role by its ID.
        /// </summary>
        RoleDto GetRoleById(int id);

        /// <summary>
        /// Gets a role by its name.
        /// </summary>
        RoleDto GetRoleByName(string name);

        /// <summary>
        /// Gets all roles.
        /// </summary>
        List<RoleDto> GetAllRoles();

        /// <summary>
        /// Adds a new role.
        /// </summary>
        bool InsertRole(RoleDto role);

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        bool UpdateRole(RoleDto role);

        /// <summary>
        /// Deletes a role by ID.
        /// </summary>
        bool DeleteRole(int id);
    }
}
