namespace myFlatLightLogin.Dal.Dto
{
    /// <summary>
    /// Defines application-level user roles for authorization.
    /// These values MUST match the Role IDs in the database (Firebase/SQLite).
    /// User = 1, Admin = 2. Additional roles can be added starting from ID 3.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Standard user with basic permissions (Database Role ID: 1).
        /// </summary>
        User = 1,

        /// <summary>
        /// Administrator with elevated permissions (e.g., view logs, manage users) (Database Role ID: 2).
        /// </summary>
        Admin = 2,

        /// <summary>
        /// Guest user who is not registered in the system and with limited permissions (Database Role ID: 3).
        /// </summary>
        Guest = 3
    }
}
