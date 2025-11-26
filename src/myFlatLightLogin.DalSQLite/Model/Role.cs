using SQLite;

namespace myFlatLightLogin.DalSQLite.Model
{
    /// <summary>
    /// SQLite Role model for storing application roles.
    /// Provides better maintainability than hardcoded enum values.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// Primary key for the role.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Role name (e.g., "Admin", "User", "Manager").
        /// </summary>
        [MaxLength(50), Unique, NotNull]
        public string? Name { get; set; }

        /// <summary>
        /// Optional description of the role.
        /// </summary>
        [MaxLength(200)]
        public string? Description { get; set; }
    }
}
