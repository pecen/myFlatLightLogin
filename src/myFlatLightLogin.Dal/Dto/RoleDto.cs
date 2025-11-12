namespace myFlatLightLogin.Dal.Dto
{
    /// <summary>
    /// Data Transfer Object for Role entity.
    /// Used to transfer role data between application layers.
    /// </summary>
    public class RoleDto
    {
        /// <summary>
        /// Role ID (1 = User, 2 = Admin).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Role name (e.g., "User", "Admin").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional description of the role.
        /// </summary>
        public string Description { get; set; }
    }
}
