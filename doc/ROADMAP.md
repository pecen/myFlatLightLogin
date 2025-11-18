# myFlatLightLogin - Development Roadmap

## Architecture Vision

This roadmap outlines the evolution of myFlatLightLogin from a simple authentication prototype to a robust, enterprise-ready fleet management platform with proper business logic layer (BLL) and dynamic role-based access control (RBAC).

---

## Iteration Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│ ITERATION 0 - FOUNDATION (✓ COMPLETED)                             │
│ Offline-First Authentication with Firebase + SQLite                │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ ITERATION 1 - VALIDATION & STABILIZATION (← CURRENT)                │
│ Complete Testing & Bug Fixes                                        │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ ITERATION 2 - BUSINESS LOGIC LAYER                                  │
│ Introduce myFlatLightLogin.Library with CSLA Patterns              │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ ITERATION 3 - DYNAMIC RBAC SYSTEM                                   │
│ Permission-Based Authorization with Role Management                │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ ITERATION 4 - FLEET MANAGEMENT CORE                                 │
│ Vehicle, Driver, and Assignment Features                           │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ ITERATION 5+ - DOMAIN MODULES                                       │
│ Maintenance, Purchase, Dispatch, Reporting, etc.                   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## ITERATION 0 - FOUNDATION ✓ COMPLETED

### Completed Features
- ✓ WPF UI with MVVM pattern
- ✓ Firebase Authentication integration (email/password)
- ✓ Firebase Realtime Database for cloud storage
- ✓ SQLite local database for offline-first architecture
- ✓ Dual DAL pattern (IUserDal, IRoleDal with SQLite + Firebase implementations)
- ✓ Network connectivity detection and Firebase reachability checks
- ✓ Automatic sync between SQLite and Firebase
- ✓ User registration with local fallback
- ✓ Login with offline capability
- ✓ Password change functionality (online and offline modes)
- ✓ Role system (User, Admin, Guest) with database seeding
- ✓ RegistrationDate tracking for user accounts
- ✓ Database-driven role metadata (eliminated hardcoded switch statements)

### Current Architecture
```
myFlatLightLogin/
├── src/
│   ├── myFlatLightLogin.UI.Wpf/          # WPF Presentation Layer
│   │   ├── MVVM/
│   │   │   ├── View/                     # XAML views
│   │   │   └── ViewModel/                # ViewModels (business rules here!)
│   │   ├── Behavior/                     # WPF behaviors
│   │   └── Converter/                    # Value converters
│   │
│   ├── myFlatLightLogin.Core/            # Shared Infrastructure
│   │   ├── Services/                     # CurrentUserService, NetworkConnectivityService
│   │   ├── MVVM/                         # Base classes (ViewModelBase, RelayCommand)
│   │   ├── Enums/                        # UserRole enum
│   │   └── Utilities/                    # Helpers
│   │
│   ├── myFlatLightLogin.Dal/             # DAL Interfaces & DTOs
│   │   ├── IUserDal.cs
│   │   ├── IRoleDal.cs
│   │   └── Dto/                          # UserDto, RoleDto
│   │
│   ├── myFlatLightLogin.DalSQLite/       # SQLite Implementation
│   │   ├── UserDal.cs
│   │   ├── RoleDal.cs
│   │   └── Model/                        # SQLite entities
│   │
│   └── myFlatLightLogin.DalFirebase/     # Firebase Implementation
│       ├── UserDal.cs
│       └── RoleDal.cs
└── doc/
    ├── README.md
    └── testing/                          # Testing guides
```

### Known Issues
⚠️ **Business logic scattered in ViewModels** - No proper BLL layer
⚠️ **Enum-based roles** - Cannot add roles without recompiling
⚠️ **No permission system** - Only role-based, no granular permissions
⚠️ **Direct DAL access from ViewModels** - No business rule validation layer

---

## ITERATION 1 - VALIDATION & STABILIZATION (CURRENT)

**Goal:** Complete testing of all implemented features, fix remaining bugs, ensure stability before architectural changes.

**Duration Estimate:** 1-2 weeks

### Tasks
1. ✓ Complete user registration testing (online and offline modes)
2. ✓ Complete user login testing (online and offline modes)
3. ⏳ Complete password change testing
4. ⏳ Test sync scenarios (offline → online, conflict resolution)
5. ⏳ Test role-based UI visibility (Admin vs User features)
6. ⏳ Performance testing with multiple users
7. ⏳ Edge case testing (network interruptions, Firebase throttling)
8. ⏳ Create comprehensive test documentation

### Exit Criteria
- All test scenarios pass
- No critical bugs remaining
- Documentation updated
- Code reviewed and cleaned up
- Ready for architectural refactoring

---

## ITERATION 2 - BUSINESS LOGIC LAYER

**Goal:** Introduce a proper Business Logic Layer using CSLA-inspired patterns to separate business rules from presentation and data access.

**Duration Estimate:** 3-4 weeks

### Architectural Changes

#### New Project Structure
```
myFlatLightLogin/
├── src/
│   ├── myFlatLightLogin.Library/         # ← NEW: Business Logic Layer
│   │   ├── BusinessObjects/              # Business entities
│   │   │   ├── User.cs                   # Business User (not DAL User)
│   │   │   ├── Role.cs                   # Business Role
│   │   │   └── ...
│   │   ├── Factory/                      # Factory methods (CSLA pattern)
│   │   │   ├── UserFactory.cs
│   │   │   └── RoleFactory.cs
│   │   ├── Rules/                        # Business rule classes
│   │   │   ├── UserBusinessRules.cs
│   │   │   └── RoleBusinessRules.cs
│   │   └── Services/                     # Business services
│   │       ├── IUserService.cs
│   │       ├── UserService.cs
│   │       └── ...
│   │
│   ├── myFlatLightLogin.UI.Wpf/
│   │   └── MVVM/ViewModel/               # Now calls BLL, not DAL
│   │
│   ├── myFlatLightLogin.Core/            # (unchanged)
│   ├── myFlatLightLogin.Dal/             # (unchanged)
│   ├── myFlatLightLogin.DalSQLite/       # (unchanged)
│   └── myFlatLightLogin.DalFirebase/     # (unchanged)
```

#### CSLA-Inspired Pattern Implementation

**Factory Methods Pattern:**
```csharp
// myFlatLightLogin.Library/Factory/UserFactory.cs
public static class UserFactory
{
    // Create new user (for registration)
    public static async Task<User> NewUserAsync()
    {
        var user = new User();
        await user.InitializeAsync();
        return user;
    }

    // Fetch existing user by ID
    public static async Task<User> GetUserAsync(int id)
    {
        var user = new User();
        await user.FetchAsync(id);
        return user;
    }

    // Fetch user by email (for login)
    public static async Task<User> GetUserByEmailAsync(string email)
    {
        var user = new User();
        await user.FetchByEmailAsync(email);
        return user;
    }

    // Delete user
    public static async Task DeleteUserAsync(int id)
    {
        var user = await GetUserAsync(id);
        await user.DeleteAsync();
    }
}
```

**Business Object Pattern:**
```csharp
// myFlatLightLogin.Library/BusinessObjects/User.cs
public class User : BusinessBase<User>
{
    // Properties with change tracking
    public int Id { get; private set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public Role Role { get; set; }

    // Business rules
    protected override void AddBusinessRules()
    {
        BusinessRules.AddRule(new EmailValidationRule(nameof(Email)));
        BusinessRules.AddRule(new PasswordStrengthRule());
        BusinessRules.AddRule(new UniqueEmailRule(nameof(Email)));
    }

    // Data access (uses DAL)
    private async Task FetchAsync(int id)
    {
        var dal = ServiceLocator.Get<IUserDal>();
        var dto = await dal.FetchAsync(id);
        LoadFromDto(dto);
    }

    public async Task SaveAsync()
    {
        // Validate business rules
        if (!IsValid)
            throw new ValidationException(BrokenRulesCollection);

        var dal = ServiceLocator.Get<IUserDal>();
        var dto = ConvertToDto();

        if (IsNew)
            await dal.InsertAsync(dto);
        else
            await dal.UpdateAsync(dto);
    }
}
```

**Business Service Pattern:**
```csharp
// myFlatLightLogin.Library/Services/UserService.cs
public class UserService : IUserService
{
    private readonly IUserDal _sqliteDal;
    private readonly IUserDal _firebaseDal;
    private readonly INetworkConnectivityService _network;

    public async Task<User> RegisterUserAsync(string name, string email, string password)
    {
        // Business logic: validate input
        ValidateRegistrationData(name, email, password);

        // Create user using factory
        var user = await UserFactory.NewUserAsync();
        user.Name = name;
        user.Email = email;
        user.SetPassword(password); // Business logic: hashing
        user.Role = await RoleFactory.GetDefaultRoleAsync(); // Business logic: default role

        // Save (handles offline-first logic)
        await user.SaveAsync();

        return user;
    }

    public async Task<User> LoginAsync(string email, string password)
    {
        // Fetch user
        var user = await UserFactory.GetUserByEmailAsync(email);

        // Business logic: validate password
        if (!user.ValidatePassword(password))
            throw new InvalidCredentialsException();

        // Business logic: update last login
        user.UpdateLastLogin();
        await user.SaveAsync();

        return user;
    }
}
```

### Refactoring Steps

#### Phase 1: Create BLL Infrastructure (Week 1)
1. Create `myFlatLightLogin.Library` project (.NET 7.0 Class Library)
2. Add NuGet dependencies (if using CSLA.NET, or build minimal CSLA-inspired base classes)
3. Create base classes:
   - `BusinessBase<T>` - Base for all business objects
   - `BusinessRuleBase` - Base for validation rules
   - `BusinessRulesCollection` - Manages rules per object
4. Create service interfaces:
   - `IUserService`
   - `IRoleService`
5. Set up dependency injection registration

#### Phase 2: Migrate User Management (Week 2)
1. Create `User` business object
2. Create `UserFactory` with CSLA-style factory methods
3. Create `UserBusinessRules` (email validation, password strength, etc.)
4. Create `UserService` implementation
5. Refactor `LoginViewModel` to use `IUserService` instead of DAL
6. Refactor `RegisterViewModel` to use `IUserService`
7. Test user registration and login

#### Phase 3: Migrate Role Management (Week 3)
1. Create `Role` business object
2. Create `RoleFactory`
3. Create `RoleService`
4. Refactor role-related ViewModels
5. Test role management features

#### Phase 4: Cleanup & Documentation (Week 4)
1. Remove business logic from ViewModels
2. Ensure all ViewModels use services, not DAL directly
3. Add XML documentation to business objects
4. Update architecture documentation
5. Create BLL usage guide for future development

### Key Principles for BLL

**Separation of Concerns:**
- **ViewModels:** UI state, commands, navigation - NO business rules
- **BLL (Services + Business Objects):** All business logic, validation, rules
- **DAL:** Pure data access - NO business logic

**Factory Methods (CSLA Pattern):**
- Never use `new User()` in ViewModels
- Always use `UserFactory.NewUserAsync()`, `UserFactory.GetUserAsync(id)`
- Factory encapsulates object creation and initialization

**Business Rules:**
- Declarative validation rules attached to business objects
- Runs automatically before save
- Can be synchronous or asynchronous (e.g., unique email check)

**Data Access:**
- Business objects use DAL internally
- ViewModels never touch DAL directly
- Offline-first logic stays in BLL, not ViewModels

### Dependencies
```
UI.Wpf  →  Library (BLL)  →  Dal (Interfaces)
                                ↑
                                |
                    ┌───────────┴───────────┐
                    │                       │
              DalSQLite                DalFirebase
```

### Exit Criteria
- All ViewModels refactored to use BLL services
- No direct DAL access from UI layer
- Business rules enforced consistently
- Comprehensive unit tests for business objects
- Documentation complete

---

## ITERATION 3 - DYNAMIC RBAC SYSTEM

**Goal:** Replace enum-based roles with a fully dynamic, permission-based authorization system suitable for enterprise fleet management.

**Duration Estimate:** 4-5 weeks

### Why This Matters for Fleet Management

Your application will need fine-grained permissions like:
- **Maintenance Technician:** Read/write maintenance schedules, read-only vehicles
- **Purchase Manager:** Manage vehicle acquisitions, disposals, budgets
- **Fleet Manager:** Full access to vehicles, assignments, reporting
- **Driver:** Limited access to assigned vehicles and schedules
- **Dispatcher:** Assign vehicles, manage routes
- **Compliance Officer:** Read-only access to all records, manage audits

Each organization (taxi company vs. rental company vs. delivery fleet) may need different role configurations. Hardcoded enums cannot support this.

### Database Schema Changes

#### New Tables

**Permissions Table:**
```sql
CREATE TABLE Permissions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,           -- e.g., "ReadVehicles"
    DisplayName TEXT NOT NULL,           -- e.g., "View Vehicles"
    Description TEXT,                     -- Human-readable description
    Module TEXT,                          -- e.g., "Vehicles", "Maintenance", "Fleet"
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
```

**RolePermissions Junction Table:**
```sql
CREATE TABLE RolePermissions (
    RoleId INTEGER NOT NULL,
    PermissionId INTEGER NOT NULL,
    GrantedAt TEXT NOT NULL,
    GrantedBy INTEGER,                    -- User ID who granted permission
    PRIMARY KEY (RoleId, PermissionId),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);
```

**Updated Users Table:**
```sql
-- Already has RoleId, no changes needed
-- RoleId references Roles.Id
```

**Updated Roles Table:**
```sql
-- Already exists, may add columns:
ALTER TABLE Roles ADD COLUMN IsSystem INTEGER DEFAULT 0;  -- Prevent deletion of system roles
ALTER TABLE Roles ADD COLUMN IsActive INTEGER DEFAULT 1;  -- Enable/disable roles
```

#### Example Permissions Seeding

```sql
-- Vehicle Management
INSERT INTO Permissions VALUES (1, 'ReadVehicles', 'View Vehicles', 'View vehicle list and details', 'Vehicles', ...);
INSERT INTO Permissions VALUES (2, 'WriteVehicles', 'Edit Vehicles', 'Create and edit vehicle records', 'Vehicles', ...);
INSERT INTO Permissions VALUES (3, 'DeleteVehicles', 'Delete Vehicles', 'Remove vehicle records', 'Vehicles', ...);

-- Maintenance
INSERT INTO Permissions VALUES (4, 'ReadMaintenance', 'View Maintenance', 'View maintenance schedules', 'Maintenance', ...);
INSERT INTO Permissions VALUES (5, 'WriteMaintenance', 'Edit Maintenance', 'Create/edit maintenance plans', 'Maintenance', ...);

-- Fleet Operations
INSERT INTO Permissions VALUES (6, 'AssignVehicles', 'Assign Vehicles', 'Assign vehicles to drivers', 'Fleet', ...);
INSERT INTO Permissions VALUES (7, 'ManageRoutes', 'Manage Routes', 'Create and modify routes', 'Fleet', ...);

-- Purchase/Finance
INSERT INTO Permissions VALUES (8, 'ReadPurchases', 'View Purchases', 'View acquisition records', 'Purchase', ...);
INSERT INTO Permissions VALUES (9, 'WritePurchases', 'Manage Purchases', 'Create purchase orders', 'Purchase', ...);
INSERT INTO Permissions VALUES (10, 'ApprovePurchases', 'Approve Purchases', 'Approve high-value purchases', 'Purchase', ...);

-- Administration
INSERT INTO Permissions VALUES (11, 'ManageUsers', 'Manage Users', 'Create/edit/delete users', 'Admin', ...);
INSERT INTO Permissions VALUES (12, 'ManageRoles', 'Manage Roles', 'Create/edit roles and permissions', 'Admin', ...);
INSERT INTO Permissions VALUES (13, 'ViewLogs', 'View System Logs', 'Access audit and system logs', 'Admin', ...);
```

#### Example Role-Permission Assignments

```sql
-- Fleet Manager role (comprehensive access)
INSERT INTO RolePermissions VALUES (4, 1, ...);  -- ReadVehicles
INSERT INTO RolePermissions VALUES (4, 2, ...);  -- WriteVehicles
INSERT INTO RolePermissions VALUES (4, 4, ...);  -- ReadMaintenance
INSERT INTO RolePermissions VALUES (4, 6, ...);  -- AssignVehicles
INSERT INTO RolePermissions VALUES (4, 7, ...);  -- ManageRoutes

-- Maintenance Technician role (limited to maintenance)
INSERT INTO RolePermissions VALUES (5, 1, ...);  -- ReadVehicles (read-only!)
INSERT INTO RolePermissions VALUES (5, 4, ...);  -- ReadMaintenance
INSERT INTO RolePermissions VALUES (5, 5, ...);  -- WriteMaintenance

-- Purchase Manager role
INSERT INTO RolePermissions VALUES (6, 8, ...);  -- ReadPurchases
INSERT INTO RolePermissions VALUES (6, 9, ...);  -- WritePurchases
INSERT INTO RolePermissions VALUES (6, 10, ...); -- ApprovePurchases

-- Admin role (full access)
INSERT INTO RolePermissions VALUES (2, 1, ...);  -- All permissions...
-- ... (grant all permissions to Admin)
```

### Code Changes

#### 1. Remove UserRole Enum
```csharp
// DELETE: myFlatLightLogin.Core/Enums/UserRole.cs
// This file will be completely removed
```

#### 2. Update Models

**Permission Model (SQLite):**
```csharp
// myFlatLightLogin.DalSQLite/Model/Permission.cs
[Table("Permissions")]
public class Permission
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique]
    public string Name { get; set; }          // e.g., "ReadVehicles"

    public string DisplayName { get; set; }   // e.g., "View Vehicles"
    public string Description { get; set; }
    public string Module { get; set; }        // e.g., "Vehicles", "Maintenance"
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }
}
```

**RolePermission Model (SQLite):**
```csharp
// myFlatLightLogin.DalSQLite/Model/RolePermission.cs
[Table("RolePermissions")]
public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public string GrantedAt { get; set; }
    public int? GrantedBy { get; set; }
}
```

**Update User Model:**
```csharp
// myFlatLightLogin.DalSQLite/Model/User.cs
public class User
{
    // ... existing properties ...

    public int RoleId { get; set; }  // Keep as int, no enum

    // Navigation property (optional, for ORM)
    [Ignore]
    public Role Role { get; set; }
}
```

**Update UserDto:**
```csharp
// myFlatLightLogin.Dal/Dto/UserDto.cs
public class UserDto
{
    // ... existing properties ...

    public int RoleId { get; set; }         // Changed from UserRole to int
    public string? RoleName { get; set; }   // Add for convenience
}
```

#### 3. Create DAL for Permissions

**Interface:**
```csharp
// myFlatLightLogin.Dal/IPermissionDal.cs
public interface IPermissionDal
{
    Task<List<PermissionDto>> FetchAllAsync();
    Task<PermissionDto?> FetchAsync(int id);
    Task<PermissionDto?> FetchByNameAsync(string name);
    Task<List<PermissionDto>> FetchByModuleAsync(string module);
    Task<List<PermissionDto>> FetchByRoleIdAsync(int roleId);
    Task<bool> InsertAsync(PermissionDto permission);
    Task<bool> UpdateAsync(PermissionDto permission);
    Task<bool> DeleteAsync(int id);
}
```

**Interface for Role-Permission Management:**
```csharp
// myFlatLightLogin.Dal/IRolePermissionDal.cs
public interface IRolePermissionDal
{
    Task<bool> GrantPermissionAsync(int roleId, int permissionId, int grantedByUserId);
    Task<bool> RevokePermissionAsync(int roleId, int permissionId);
    Task<List<PermissionDto>> GetRolePermissionsAsync(int roleId);
    Task<bool> RoleHasPermissionAsync(int roleId, string permissionName);
}
```

#### 4. Create Authorization Service

**Interface:**
```csharp
// myFlatLightLogin.Library/Services/IAuthorizationService.cs
public interface IAuthorizationService
{
    /// <summary>
    /// Check if current user has a specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(string permissionName);

    /// <summary>
    /// Check if a specific user has a permission
    /// </summary>
    Task<bool> UserHasPermissionAsync(int userId, string permissionName);

    /// <summary>
    /// Get all permissions for current user
    /// </summary>
    Task<List<Permission>> GetCurrentUserPermissionsAsync();

    /// <summary>
    /// Check multiple permissions (returns true if user has ANY)
    /// </summary>
    Task<bool> HasAnyPermissionAsync(params string[] permissionNames);

    /// <summary>
    /// Check multiple permissions (returns true if user has ALL)
    /// </summary>
    Task<bool> HasAllPermissionsAsync(params string[] permissionNames);

    /// <summary>
    /// Throw exception if user lacks permission
    /// </summary>
    Task RequirePermissionAsync(string permissionName);
}
```

**Implementation:**
```csharp
// myFlatLightLogin.Library/Services/AuthorizationService.cs
public class AuthorizationService : IAuthorizationService
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRolePermissionDal _rolePermissionDal;
    private Dictionary<string, bool> _permissionCache;

    public async Task<bool> HasPermissionAsync(string permissionName)
    {
        // Check cache first
        if (_permissionCache.TryGetValue(permissionName, out var cached))
            return cached;

        // Query database
        var roleId = _currentUser.User.RoleId;
        var hasPermission = await _rolePermissionDal.RoleHasPermissionAsync(roleId, permissionName);

        // Cache result
        _permissionCache[permissionName] = hasPermission;

        return hasPermission;
    }

    public async Task RequirePermissionAsync(string permissionName)
    {
        if (!await HasPermissionAsync(permissionName))
            throw new UnauthorizedAccessException($"Permission required: {permissionName}");
    }
}
```

#### 5. Update ViewModels to Use Authorization

**Example - Vehicle Management ViewModel:**
```csharp
public class VehicleListViewModel : ViewModelBase
{
    private readonly IAuthorizationService _authService;
    private readonly IVehicleService _vehicleService;

    public bool CanEditVehicles { get; private set; }
    public bool CanDeleteVehicles { get; private set; }

    public async Task InitializeAsync()
    {
        // Check permissions
        CanEditVehicles = await _authService.HasPermissionAsync("WriteVehicles");
        CanDeleteVehicles = await _authService.HasPermissionAsync("DeleteVehicles");

        // Load data
        Vehicles = await _vehicleService.GetAllVehiclesAsync();
    }

    public async Task DeleteVehicleAsync(int vehicleId)
    {
        // Enforce permission
        await _authService.RequirePermissionAsync("DeleteVehicles");

        await _vehicleService.DeleteVehicleAsync(vehicleId);
    }
}
```

**Example - Admin Role Management:**
```csharp
public class RoleManagementViewModel : ViewModelBase
{
    private readonly IAuthorizationService _authService;
    private readonly IRoleService _roleService;

    public async Task InitializeAsync()
    {
        // Only admins can manage roles
        await _authService.RequirePermissionAsync("ManageRoles");

        await LoadRolesAsync();
    }

    public async Task CreateRoleAsync(string name, string description)
    {
        await _authService.RequirePermissionAsync("ManageRoles");

        var role = await RoleFactory.NewRoleAsync();
        role.Name = name;
        role.Description = description;
        await role.SaveAsync();
    }

    public async Task AssignPermissionAsync(int roleId, int permissionId)
    {
        await _authService.RequirePermissionAsync("ManageRoles");

        await _rolePermissionDal.GrantPermissionAsync(roleId, permissionId, _currentUser.User.Id);
    }
}
```

### Migration Strategy

#### Step 1: Database Migration
1. Create migration script to add Permissions and RolePermissions tables
2. Seed initial permissions (vehicle, maintenance, purchase, admin modules)
3. Migrate existing users:
   - Users with `RoleId = 2` (Admin) → Grant all permissions
   - Users with `RoleId = 1` (User) → Grant basic read permissions
   - Users with `RoleId = 3` (Guest) → Grant minimal read-only permissions
4. Add IsSystem flag to built-in roles to prevent accidental deletion

#### Step 2: Code Migration
1. Remove `UserRole` enum
2. Update all `UserDto.Role` from `UserRole` to `int RoleId`
3. Create `Permission`, `RolePermission` models and DALs
4. Implement `IAuthorizationService`
5. Update ViewModels to check permissions instead of roles
6. Remove hardcoded role checks like `if (user.Role == UserRole.Admin)`

#### Step 3: UI Updates
1. Create **Role Management UI** (Admin only):
   - List all roles
   - Create/edit/delete roles
   - View role details
2. Create **Permission Assignment UI** (Admin only):
   - Show all permissions grouped by module
   - Checkboxes to grant/revoke permissions per role
   - Visual indicator of inherited permissions
3. Update existing views to respect new permissions

### Refactoring Steps (Week by Week)

**Week 1: Database & DAL**
1. Design and implement Permissions and RolePermissions tables
2. Create Permission and RolePermission models
3. Implement IPermissionDal and IRolePermissionDal (SQLite)
4. Implement Firebase equivalents
5. Create seed data for initial permissions
6. Write database migration scripts

**Week 2: Authorization Service**
1. Remove UserRole enum
2. Update UserDto and all references
3. Implement IAuthorizationService
4. Create permission caching mechanism
5. Add permission validation helpers
6. Unit test authorization service

**Week 3: BLL Integration**
1. Add authorization checks to business services
2. Update factory methods to enforce permissions
3. Add permission validation to business rules
4. Refactor existing business objects to use authorization

**Week 4: UI Updates - Admin Features**
1. Create RoleManagementViewModel
2. Create RoleManagementView (XAML)
3. Create PermissionAssignmentViewModel
4. Create PermissionAssignmentView (XAML)
5. Add admin menu items

**Week 5: UI Updates - Existing Features**
1. Update all ViewModels to use IAuthorizationService
2. Remove hardcoded role checks
3. Add permission-based UI element visibility
4. Test all features with different role configurations
5. Documentation and cleanup

### Permission Naming Convention

Use a consistent naming scheme:
- **Read[Module]** - View/list entities (e.g., ReadVehicles, ReadMaintenance)
- **Write[Module]** - Create/edit entities (e.g., WriteVehicles, WriteMaintenance)
- **Delete[Module]** - Remove entities (e.g., DeleteVehicles)
- **Approve[Action]** - Approval workflows (e.g., ApprovePurchases)
- **Manage[Feature]** - Full control (e.g., ManageUsers, ManageRoles)
- **View[SpecialFeature]** - Special read access (e.g., ViewLogs, ViewReports)

### Exit Criteria
- UserRole enum completely removed
- All role checks replaced with permission checks
- Admin can create custom roles through UI
- Admin can assign permissions to roles
- All ViewModels enforce permissions
- Existing features work with new system
- Migration guide for existing data
- Documentation complete

---

## ITERATION 4 - FLEET MANAGEMENT CORE

**Goal:** Implement core fleet management features (vehicles, drivers, assignments).

**Duration Estimate:** 6-8 weeks

### Domain Model

**Core Entities:**
- **Vehicle** - Make, model, year, VIN, license plate, status, odometer
- **Driver** - Name, license number, license expiry, status
- **Assignment** - Links driver to vehicle for a time period
- **Location** - GPS tracking, geofencing
- **FuelLog** - Fuel consumption tracking
- **Incident** - Accidents, violations, damage reports

### Features
1. Vehicle management (CRUD)
2. Driver management (CRUD)
3. Vehicle-to-driver assignments
4. Basic reporting (vehicles by status, driver assignments)
5. Search and filtering

### Architecture
- Business objects in BLL (Vehicle, Driver, Assignment)
- DAL implementations (SQLite + Firebase)
- ViewModels with permission checks
- XAML views for each feature

---

## ITERATION 5+ - DOMAIN MODULES

**Goal:** Add specialized modules for different fleet management domains.

### Potential Modules (Prioritize Based on Market Research)

**Maintenance Module (High Priority)**
- Maintenance schedules
- Service history
- Part inventory
- Maintenance alerts
- Vendor management

**Purchase/Disposal Module**
- Vehicle acquisition workflow
- Depreciation tracking
- Sale/disposal management
- Budget management
- ROI analysis

**Dispatch Module (For Delivery/Taxi Fleets)**
- Route planning
- Real-time vehicle tracking
- Job assignment
- Delivery/ride history

**Compliance Module**
- Inspection tracking
- License/registration renewals
- Insurance management
- Regulatory compliance

**Reporting & Analytics Module**
- Custom report builder
- Dashboard with KPIs
- Cost analysis
- Utilization reports
- Export to Excel/PDF

**Mobile Companion App**
- Driver mobile app (view assignments, log fuel, report incidents)
- Manager mobile app (fleet overview, approvals)

---

## Technology Stack Evolution

### Current (Iteration 0-1)
- **.NET 7.0** - WPF, Class Libraries
- **SQLite** - Local database
- **Firebase** - Authentication, Realtime Database
- **Serilog** - Logging
- **MVVM** - Presentation pattern

### Planned Additions (Iteration 2+)
- **CSLA.NET** (or custom CSLA-inspired base classes) - BLL framework
- **FluentValidation** - Advanced validation rules
- **AutoMapper** - DTO ↔ Business Object mapping
- **Unit Testing** - xUnit, Moq, FluentAssertions
- **Swagger/OpenAPI** (if adding REST API for mobile apps)

### Future Considerations (Iteration 5+)
- **Blazor Server/WASM** - Web version of the app
- **MAUI** - Cross-platform mobile apps (iOS, Android)
- **SignalR** - Real-time updates (vehicle tracking)
- **Azure Functions** - Serverless background jobs (sync, reports)
- **Stripe/PayPal** - Payment processing for subscription model
- **Twilio** - SMS notifications for drivers

---

## Dependency Graph

```
┌────────────────────────────────────────────────────────────────────┐
│                         MUST COMPLETE FIRST                        │
└────────────────────────────────────────────────────────────────────┘
                                  │
        ┌─────────────────────────┼─────────────────────────┐
        │                         │                         │
        ▼                         ▼                         ▼
┌──────────────┐          ┌──────────────┐          ┌──────────────┐
│ Iteration 1  │          │              │          │              │
│   Testing    │──────────▶ Iteration 2  │──────────▶ Iteration 3  │
│              │          │     BLL      │          │  RBAC System │
└──────────────┘          └──────────────┘          └──────────────┘
                                                            │
                    ┌───────────────────────────────────────┘
                    │
                    ▼
            ┌──────────────┐
            │ Iteration 4  │
            │Fleet Core    │
            └──────────────┘
                    │
                    ▼
            ┌──────────────┐
            │ Iteration 5+ │
            │  Modules     │
            └──────────────┘
```

**Critical Path:**
- Cannot do BLL without stable foundation (Iteration 1)
- Cannot do RBAC properly without BLL (services enforce permissions)
- Cannot do Fleet features without RBAC (features need permission checks)

---

## Risk Management

### Technical Risks

**Risk 1: CSLA Learning Curve**
- **Mitigation:** Start with simplified CSLA-inspired patterns, not full CSLA.NET framework
- **Fallback:** Build custom BusinessBase classes if CSLA.NET is too complex

**Risk 2: Firebase Scaling Limits**
- **Mitigation:** Monitor usage, plan migration to Firestore or Azure SQL if needed
- **Threshold:** Firebase Realtime Database max: 200k concurrent connections, 1GB free storage

**Risk 3: Offline Sync Conflicts**
- **Mitigation:** Implement conflict resolution strategy (last-write-wins vs. manual merge)
- **Test:** Extensive testing of offline scenarios in Iteration 1

**Risk 4: Performance with Large Datasets**
- **Mitigation:** Implement pagination, lazy loading, database indexing
- **Benchmark:** Test with 10k+ vehicles, 1k+ users

### Business Risks

**Risk 1: Scope Creep**
- **Mitigation:** Stick to iteration plan, defer non-critical features
- **Review:** Monthly roadmap review to re-prioritize

**Risk 2: Market Fit**
- **Mitigation:** Validate with potential customers before building every module
- **Strategy:** Build Maintenance module first (universal need), then prioritize by feedback

---

## Success Metrics

### Iteration 1 (Testing)
- [ ] 100% of test scenarios pass
- [ ] Zero critical bugs
- [ ] All edge cases documented

### Iteration 2 (BLL)
- [ ] Zero business logic in ViewModels
- [ ] 100% of data access goes through BLL
- [ ] Factory pattern used consistently
- [ ] Business rules coverage > 80%

### Iteration 3 (RBAC)
- [ ] Enum-based roles completely removed
- [ ] Admin can create roles through UI
- [ ] Permission checks in all sensitive operations
- [ ] Zero hardcoded role checks

### Iteration 4 (Fleet Core)
- [ ] CRUD operations for vehicles, drivers, assignments
- [ ] Permission-based access to features
- [ ] Offline-first for all operations
- [ ] User feedback: "Easy to use" rating > 4/5

### Iteration 5+ (Modules)
- [ ] At least 3 specialized modules deployed
- [ ] Customer acquisition: 10+ paying organizations
- [ ] Feature requests tracked and prioritized

---

## Notes for Future Development

### Code Organization Best Practices

**Naming Conventions:**
- Business Objects: `User`, `Vehicle`, `Driver` (no "BO" suffix)
- DTOs: `UserDto`, `VehicleDto` (keep "Dto" suffix)
- ViewModels: `UserListViewModel`, `VehicleEditViewModel`
- Services: `IUserService`, `UserService`
- DALs: `IUserDal`, `UserDal`

**Folder Structure:**
```
myFlatLightLogin.Library/
├── BusinessObjects/
│   ├── User.cs
│   ├── Vehicle.cs
│   └── ...
├── Factory/
│   ├── UserFactory.cs
│   ├── VehicleFactory.cs
│   └── ...
├── Rules/
│   ├── Common/
│   │   ├── EmailValidationRule.cs
│   │   └── RequiredFieldRule.cs
│   └── User/
│       ├── PasswordStrengthRule.cs
│       └── UniqueEmailRule.cs
├── Services/
│   ├── IUserService.cs
│   ├── UserService.cs
│   ├── IAuthorizationService.cs
│   └── AuthorizationService.cs
└── Exceptions/
    ├── BusinessRuleException.cs
    └── UnauthorizedAccessException.cs
```

### Testing Strategy

**Unit Tests:**
- Business rules (90%+ coverage)
- Factory methods
- Service methods (mock DAL)

**Integration Tests:**
- DAL implementations
- Offline sync scenarios
- Permission enforcement

**UI Tests:**
- Manual testing guide for each iteration
- Automated UI tests (Coded UI or WPF testing framework) for critical paths

### Documentation Standards

**Code Documentation:**
- XML comments on all public APIs
- Business rules documented in code
- Complex algorithms explained

**User Documentation:**
- User manual (PDF/web)
- Admin guide (role/permission setup)
- Video tutorials for key features

**Developer Documentation:**
- Architecture decision records (ADR)
- API documentation (if REST API added)
- Setup/deployment guide

---

## Next Steps (Immediate Actions)

### For Iteration 1 (Current)
1. ✓ Complete password change testing
2. ⏳ Test all sync scenarios
3. ⏳ Document test results
4. ⏳ Fix any remaining bugs
5. ⏳ Code review and cleanup

### Preparation for Iteration 2 (BLL)
1. Research CSLA.NET (review documentation, samples)
2. Decide: Full CSLA.NET vs. CSLA-inspired custom base classes
3. Set up myFlatLightLogin.Library project structure
4. Install required NuGet packages
5. Create initial base classes (BusinessBase, BusinessRuleBase)

### Long-term Planning
1. Market research: Which fleet management features are most valuable?
2. Competitor analysis: What do existing solutions offer?
3. Pricing model: Subscription tiers (Free, Pro, Enterprise)?
4. Roadmap refinement based on customer feedback

---

## Conclusion

This roadmap provides a clear path from the current authentication prototype to a full-featured, enterprise-ready fleet management platform. Each iteration builds on the previous one, ensuring solid foundations before adding complexity.

**Key Principles:**
- **Incremental:** Small, manageable iterations
- **Tested:** Each iteration fully tested before moving forward
- **Flexible:** Roadmap can adapt based on feedback and market needs
- **Quality-focused:** No shortcuts - proper architecture from the start

**Timeline Summary:**
- **Iteration 1:** 1-2 weeks (Testing & Stabilization)
- **Iteration 2:** 3-4 weeks (BLL Implementation)
- **Iteration 3:** 4-5 weeks (Dynamic RBAC)
- **Iteration 4:** 6-8 weeks (Fleet Core)
- **Iteration 5+:** Ongoing (Domain Modules)

**Total to MVP (Iteration 4):** ~4-5 months of focused development

Good luck with your development journey! 🚀
