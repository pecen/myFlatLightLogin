using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.DalSQLite.Model;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace myFlatLightLogin.DalSQLite
{
    /// <summary>
    /// Data Access Layer for Role operations in SQLite.
    /// </summary>
    public class RoleDal : IRoleDal
    {
        private static readonly ILogger _logger = Log.ForContext<RoleDal>();
        private readonly string _dbPath;

        public RoleDal()
        {
            _dbPath = Path.Combine(Environment.CurrentDirectory, "security.db3");
            InitializeDatabase();
        }

        /// <summary>
        /// Initializes the SQLite role data store asynchronously.
        /// Seeds default roles if they don't exist.
        /// </summary>
        public Task InitializeAsync()
        {
            _logger.Information("Initializing SQLite roles database at {DbPath}...", _dbPath);

            // SQLite initialization is already done synchronously in constructor,
            // but we provide this method for interface compliance and explicit initialization
            using (var conn = new SQLiteConnection(_dbPath))
            {
                var roleCount = conn.Table<Role>().Count();
                _logger.Information("SQLite roles initialized successfully. Found {RoleCount} roles", roleCount);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the database and seeds default roles if needed.
        /// </summary>
        private void InitializeDatabase()
        {
            _logger.Debug("Creating SQLite roles table if not exists...");

            using (var conn = new SQLiteConnection(_dbPath))
            {
                conn.CreateTable<Role>();

                // Seed default roles if table is empty
                if (conn.Table<Role>().Count() == 0)
                {
                    _logger.Information("No roles found in SQLite, seeding default roles...");
                    SeedDefaultRoles(conn);
                }
            }
        }

        /// <summary>
        /// Seeds the default roles (User, Admin, and Guest).
        /// Role IDs must match the UserRole enum values.
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

            var guestRole = new Role
            {
                Id = 3,
                Name = "Guest",
                Description = "Guest user with limited permissions (read-only access)"
            };

            _logger.Debug("Seeding 'User' role to SQLite...");
            conn.Insert(userRole);
            _logger.Debug("Seeding 'Admin' role to SQLite...");
            conn.Insert(adminRole);
            _logger.Debug("Seeding 'Guest' role to SQLite...");
            conn.Insert(guestRole);

            _logger.Information("Default roles seeded to SQLite successfully");
        }

        /// <summary>
        /// Gets a role by its ID.
        /// </summary>
        public RoleDto? Fetch(int id)
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
        public RoleDto Fetch(string name)
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
        public List<RoleDto> Fetch()
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                return conn.Table<Role>().ToList().Select(ConvertToDto).ToList();
            }
        }

        /// <summary>
        /// Adds a new role.
        /// </summary>
        public bool Insert(RoleDto roleDto)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                var role = ConvertToModel(roleDto);
                int rowsAffected = conn.Insert(role);

                // Update the DTO with the auto-generated ID from SQLite
                // This is critical so Firebase gets the correct ID
                roleDto.Id = role.Id;

                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        public bool Update(RoleDto roleDto)
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
        public bool Delete(int id)
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
                Name = roleDto.Name ?? string.Empty,
                Description = roleDto.Description ?? string.Empty
            };
        }

        #endregion
    }
}
