using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.DalSQLite.Model;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace myFlatLightLogin.DalSQLite
{
    /// <summary>
    /// Data Access Layer for Role operations in SQLite.
    /// </summary>
    public class RoleDal : IRoleDal
    {
        private readonly string _dbPath;

        public RoleDal()
        {
            _dbPath = Path.Combine(Environment.CurrentDirectory, "security.db3");
            InitializeDatabase();
        }

        /// <summary>
        /// Initializes the database and seeds default roles if needed.
        /// </summary>
        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.CreateTable<Role>();

                // Seed default roles if table is empty
                if (conn.Table<Role>().Count() == 0)
                {
                    SeedDefaultRoles(conn);
                }
            }
        }

        /// <summary>
        /// Seeds the default roles (User and Admin).
        /// </summary>
        private void SeedDefaultRoles(SQLiteConnection conn)
        {
            var userRole = new Role
            {
                Id = 1,
                Name = "User",
                Description = "Standard user with basic permissions"
            };

            var adminRole = new Role
            {
                Id = 2,
                Name = "Admin",
                Description = "Administrator with elevated permissions (e.g., view logs, manage users)"
            };

            conn.Insert(userRole);
            conn.Insert(adminRole);
        }

        /// <summary>
        /// Gets a role by its ID.
        /// </summary>
        public RoleDto GetRoleById(int id)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                var role = conn.Table<Role>().FirstOrDefault(r => r.Id == id);
                return role != null ? ConvertToDto(role) : null;
            }
        }

        /// <summary>
        /// Gets a role by its name.
        /// </summary>
        public RoleDto GetRoleByName(string name)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                var role = conn.Table<Role>().FirstOrDefault(r => r.Name == name);
                return role != null ? ConvertToDto(role) : null;
            }
        }

        /// <summary>
        /// Gets all roles.
        /// </summary>
        public List<RoleDto> GetAllRoles()
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                return conn.Table<Role>().ToList().Select(ConvertToDto).ToList();
            }
        }

        /// <summary>
        /// Adds a new role.
        /// </summary>
        public bool InsertRole(RoleDto roleDto)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                var role = ConvertToModel(roleDto);
                int rowsAffected = conn.Insert(role);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        public bool UpdateRole(RoleDto roleDto)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                var role = ConvertToModel(roleDto);
                int rowsAffected = conn.Update(role);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Deletes a role by ID.
        /// </summary>
        public bool DeleteRole(int id)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                int rowsAffected = conn.Delete<Role>(id);
                return rowsAffected > 0;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Converts a Role model to RoleDto.
        /// </summary>
        private RoleDto ConvertToDto(Role role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            };
        }

        /// <summary>
        /// Converts a RoleDto to Role model.
        /// </summary>
        private Role ConvertToModel(RoleDto roleDto)
        {
            return new Role
            {
                Id = roleDto.Id,
                Name = roleDto.Name,
                Description = roleDto.Description
            };
        }

        #endregion
    }
}
