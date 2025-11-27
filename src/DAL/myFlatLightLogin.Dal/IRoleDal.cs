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
        RoleDto Fetch(int id);

        /// <summary>
        /// Gets a role by its name.
        /// </summary>
        RoleDto Fetch(string name);

        /// <summary>
        /// Gets all roles.
        /// </summary>
        List<RoleDto> Fetch();

        /// <summary>
        /// Adds a new role.
        /// </summary>
        bool Insert(RoleDto role);

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        bool Update(RoleDto role);

        /// <summary>
        /// Deletes a role by ID.
        /// </summary>
        bool Delete(int id);
    }
}
