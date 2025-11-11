namespace myFlatLightLogin.Dal.Dto
{
    /// <summary>
    /// Defines application-level user roles for authorization.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Standard user with basic permissions.
        /// </summary>
        User = 0,

        /// <summary>
        /// Administrator with elevated permissions (e.g., view logs, manage users).
        /// </summary>
        Admin = 1
    }
}
