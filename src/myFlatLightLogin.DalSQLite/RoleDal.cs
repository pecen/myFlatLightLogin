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
    public class RoleDal
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
        public Role GetRoleById(int id)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                return conn.Table<Role>().FirstOrDefault(r => r.Id == id);
            }
        }

        /// <summary>
        /// Gets a role by its name.
        /// </summary>
        public Role GetRoleByName(string name)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                return conn.Table<Role>().FirstOrDefault(r => r.Name == name);
            }
        }

        /// <summary>
        /// Gets all roles.
        /// </summary>
        public List<Role> GetAllRoles()
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                return conn.Table<Role>().ToList();
            }
        }

        /// <summary>
        /// Adds a new role.
        /// </summary>
        public bool InsertRole(Role role)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
                int rowsAffected = conn.Insert(role);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        public bool UpdateRole(Role role)
        {
            using (var conn = new SQLiteConnection(_dbPath))
            {
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
    }
}
