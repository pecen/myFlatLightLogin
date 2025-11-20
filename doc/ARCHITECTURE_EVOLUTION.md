# Architecture Evolution - Visual Guide

This document provides visual representations of how the architecture evolves through each iteration.

---

## Current Architecture (Iteration 0 - Completed)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                            │
│                      myFlatLightLogin.UI.Wpf                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   ┌──────────────┐      ┌──────────────┐      ┌──────────────┐       │
│   │   LoginView  │      │ RegisterView │      │ProfileEditView│       │
│   └──────┬───────┘      └──────┬───────┘      └──────┬───────┘       │
│          │                     │                      │                │
│          ▼                     ▼                      ▼                │
│   ┌──────────────┐      ┌──────────────┐      ┌──────────────┐       │
│   │LoginViewModel│      │RegisterVM    │      │ProfileEditVM │       │
│   │              │      │              │      │              │       │
│   │ ⚠️ BUSINESS  │      │ ⚠️ BUSINESS  │      │ ⚠️ BUSINESS  │       │
│   │    LOGIC     │      │    LOGIC     │      │    LOGIC     │       │
│   │    HERE!     │      │    HERE!     │      │    HERE!     │       │
│   └──────┬───────┘      └──────┬───────┘      └──────┬───────┘       │
│          │                     │                      │                │
│          └─────────────────────┼──────────────────────┘                │
│                                │                                       │
└────────────────────────────────┼───────────────────────────────────────┘
                                 │
                                 │ Direct DAL Access ⚠️
                                 │
┌────────────────────────────────┼───────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                              │
│                    myFlatLightLogin.Core                              │
├────────────────────────────────┼───────────────────────────────────────┤
│                                ▼                                       │
│   ┌──────────────────────────────────────────────────────┐           │
│   │        CurrentUserService (Global State)             │           │
│   │        NetworkConnectivityService                    │           │
│   │        ViewModelBase, RelayCommand                   │           │
│   └──────────────────────────────────────────────────────┘           │
│                                                                        │
│   ┌──────────────┐                                                    │
│   │  UserRole    │  ← Enum (User=1, Admin=2, Guest=3)                │
│   │   (Enum)     │     ⚠️ Hardcoded, requires recompile to change    │
│   └──────────────┘                                                    │
└────────────────────────────────┬───────────────────────────────────────┘
                                 │
┌────────────────────────────────┼───────────────────────────────────────┐
│                         DATA ACCESS LAYER                              │
│                    myFlatLightLogin.Dal                               │
├────────────────────────────────┼───────────────────────────────────────┤
│                                ▼                                       │
│   ┌─────────────┐         ┌─────────────┐                            │
│   │ IUserDal    │         │ IRoleDal    │                            │
│   └─────────────┘         └─────────────┘                            │
│          ▲                        ▲                                   │
│          │                        │                                   │
│    ┌─────┴────────┐         ┌─────┴────────┐                        │
│    │              │         │              │                        │
└────┼──────────────┼─────────┼──────────────┼────────────────────────┘
     │              │         │              │
     │              │         │              │
┌────┼──────────────┼─────────┼──────────────┼────────────────────────┐
│    ▼              │         ▼              │                        │
│ ┌──────────┐      │    ┌──────────┐        │                        │
│ │UserDal   │      │    │RoleDal   │        │  myFlatLightLogin.     │
│ │(SQLite)  │      │    │(SQLite)  │        │  DalSQLite             │
│ └──────────┘      │    └──────────┘        │                        │
│      │            │         │              │                        │
│      ▼            │         ▼              │                        │
│ ┌──────────┐      │    ┌──────────┐        │                        │
│ │  SQLite  │      │    │  Roles   │        │                        │
│ │ Database │      │    │  Table   │        │                        │
│ └──────────┘      │    └──────────┘        │                        │
└───────────────────┼─────────────────────────┼────────────────────────┘
                    │                         │
┌───────────────────┼─────────────────────────┼────────────────────────┐
│                   ▼                         ▼                        │
│ ┌──────────┐                         ┌──────────┐                   │
│ │UserDal   │                         │RoleDal   │  myFlatLightLogin. │
│ │(Firebase)│                         │(Firebase)│  DalFirebase       │
│ └──────────┘                         └──────────┘                   │
│      │                                     │                         │
│      ▼                                     ▼                         │
│ ┌──────────┐                         ┌──────────┐                   │
│ │ Firebase │                         │ Firebase │                   │
│ │   Auth   │                         │Realtime  │                   │
│ │          │                         │ Database │                   │
│ └──────────┘                         └──────────┘                   │
└────────────────────────────────────────────────────────────────────────┘

PROBLEMS:
⚠️ Business logic scattered in ViewModels (validation, rules, workflows)
⚠️ ViewModels directly access DAL (tight coupling)
⚠️ UserRole enum requires recompilation to add roles
⚠️ No granular permissions (only role-based)
⚠️ Difficult to unit test (ViewModels depend on DAL)
```

---

## After Iteration 2: Business Logic Layer Added

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                            │
│                      myFlatLightLogin.UI.Wpf                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   ┌──────────────┐      ┌──────────────┐      ┌──────────────┐       │
│   │   LoginView  │      │ RegisterView │      │ProfileEditView│       │
│   └──────┬───────┘      └──────┬───────┘      └──────┬───────┘       │
│          │                     │                      │                │
│          ▼                     ▼                      ▼                │
│   ┌──────────────┐      ┌──────────────┐      ┌──────────────┐       │
│   │LoginViewModel│      │RegisterVM    │      │ProfileEditVM │       │
│   │              │      │              │      │              │       │
│   │ ✓ UI State   │      │ ✓ UI State   │      │ ✓ UI State   │       │
│   │ ✓ Commands   │      │ ✓ Commands   │      │ ✓ Commands   │       │
│   │ ✓ Navigation │      │ ✓ Navigation │      │ ✓ Navigation │       │
│   │ ❌ NO Logic! │      │ ❌ NO Logic! │      │ ❌ NO Logic! │       │
│   └──────┬───────┘      └──────┬───────┘      └──────┬───────┘       │
│          │                     │                      │                │
│          └─────────────────────┼──────────────────────┘                │
│                                │                                       │
│                                │ Calls Services ✓                      │
│                                │                                       │
└────────────────────────────────┼───────────────────────────────────────┘
                                 │
┌────────────────────────────────┼───────────────────────────────────────┐
│                      BUSINESS LOGIC LAYER  ← NEW!                      │
│                    myFlatLightLogin.Library                           │
├────────────────────────────────┼───────────────────────────────────────┤
│                                ▼                                       │
│   ┌────────────────────────────────────────────────────┐             │
│   │              BUSINESS SERVICES                      │             │
│   ├────────────────────────────────────────────────────┤             │
│   │ IUserService          UserService                  │             │
│   │ IRoleService          RoleService                  │             │
│   │ IAuthService          AuthService                  │             │
│   └────────────────┬───────────────────────────────────┘             │
│                    │                                                  │
│   ┌────────────────┴───────────────────────────────────┐             │
│   │            BUSINESS OBJECTS (Entities)             │             │
│   ├────────────────────────────────────────────────────┤             │
│   │ User          ← Smart objects with:                │             │
│   │ Role            - Business rules                   │             │
│   │                 - Validation logic                 │             │
│   │                 - Change tracking                  │             │
│   │                 - Save/Fetch methods               │             │
│   └────────────────┬───────────────────────────────────┘             │
│                    │                                                  │
│   ┌────────────────┴───────────────────────────────────┐             │
│   │             FACTORY METHODS (CSLA Pattern)         │             │
│   ├────────────────────────────────────────────────────┤             │
│   │ UserFactory.NewUserAsync()                         │             │
│   │ UserFactory.GetUserAsync(id)                       │             │
│   │ UserFactory.GetUserByEmailAsync(email)             │             │
│   │ RoleFactory.NewRoleAsync()                         │             │
│   └────────────────┬───────────────────────────────────┘             │
│                    │                                                  │
│   ┌────────────────┴───────────────────────────────────┐             │
│   │               BUSINESS RULES                       │             │
│   ├────────────────────────────────────────────────────┤             │
│   │ EmailValidationRule                                │             │
│   │ PasswordStrengthRule                               │             │
│   │ UniqueEmailRule                                    │             │
│   │ RolePermissionRule                                 │             │
│   └────────────────┬───────────────────────────────────┘             │
│                    │                                                  │
└────────────────────┼──────────────────────────────────────────────────┘
                     │
                     │ Uses DAL ✓
                     │
┌────────────────────┼──────────────────────────────────────────────────┐
│                    ▼       DATA ACCESS LAYER                          │
│                    myFlatLightLogin.Dal + Implementations             │
├───────────────────────────────────────────────────────────────────────┤
│   IUserDal, IRoleDal                                                  │
│   UserDal (SQLite), UserDal (Firebase)                                │
│   RoleDal (SQLite), RoleDal (Firebase)                                │
└───────────────────────────────────────────────────────────────────────┘

IMPROVEMENTS:
✓ Business logic centralized in BLL
✓ ViewModels are thin (UI state only)
✓ Easy to unit test (mock services)
✓ Factory pattern enforces object creation standards
✓ Business rules declarative and reusable
✓ Clear separation of concerns

REMAINING ISSUES:
⚠️ Still using UserRole enum (hardcoded roles)
⚠️ No permission-based authorization yet
```

---

## After Iteration 3: Dynamic RBAC System Added

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                            │
│                      myFlatLightLogin.UI.Wpf                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐     │
│   │ VehicleList  │   │ MaintenanceVM│   │RoleManagementVM      │     │
│   │  ViewModel   │   │              │   │   (Admin Only)       │     │
│   └──────┬───────┘   └──────┬───────┘   └──────┬───────────────┘     │
│          │                  │                   │                      │
│          │ Checks:          │ Checks:           │ Checks:              │
│          │ ReadVehicles     │ WriteMaintenance  │ ManageRoles          │
│          │ WriteVehicles    │ ReadVehicles      │                      │
│          │                  │                   │                      │
└──────────┼──────────────────┼───────────────────┼──────────────────────┘
           │                  │                   │
           └──────────────────┼───────────────────┘
                              │
                              │ Calls Authorization Service
                              │
┌──────────────────────────────┼───────────────────────────────────────────┐
│                BUSINESS LOGIC LAYER                                     │
│              myFlatLightLogin.Library                                  │
├──────────────────────────────┼───────────────────────────────────────────┤
│                              ▼                                          │
│   ┌─────────────────────────────────────────────────────────┐         │
│   │        IAuthorizationService  ← NEW!                    │         │
│   ├─────────────────────────────────────────────────────────┤         │
│   │ HasPermissionAsync(permissionName)                      │         │
│   │ RequirePermissionAsync(permissionName)                  │         │
│   │ GetCurrentUserPermissionsAsync()                        │         │
│   │ HasAnyPermissionAsync(...)                              │         │
│   │ HasAllPermissionsAsync(...)                             │         │
│   └─────────────────────┬───────────────────────────────────┘         │
│                         │                                              │
│   ┌─────────────────────┴───────────────────────────────────┐         │
│   │           Business Services (Updated)                   │         │
│   ├─────────────────────────────────────────────────────────┤         │
│   │ VehicleService  ← Checks permissions before operations  │         │
│   │ MaintenanceService                                      │         │
│   │ UserService                                             │         │
│   └─────────────────────┬───────────────────────────────────┘         │
│                         │                                              │
│   ┌─────────────────────┴───────────────────────────────────┐         │
│   │           Business Objects (Updated)                    │         │
│   ├─────────────────────────────────────────────────────────┤         │
│   │ User   ← Now has int RoleId (not enum)                 │         │
│   │ Role   ← New: Permissions collection                   │         │
│   │ Permission ← NEW!                                       │         │
│   │ Vehicle ← NEW! (for fleet management)                  │         │
│   └─────────────────────────────────────────────────────────┘         │
│                                                                        │
└────────────────────────────┬───────────────────────────────────────────┘
                             │
┌────────────────────────────┼───────────────────────────────────────────┐
│                 DATA ACCESS LAYER (Expanded)                           │
│                 myFlatLightLogin.Dal                                  │
├────────────────────────────┼───────────────────────────────────────────┤
│                            ▼                                           │
│   ┌────────────────────────────────────────────────────┐             │
│   │ Existing:                                          │             │
│   │ - IUserDal, IRoleDal                               │             │
│   │                                                    │             │
│   │ NEW:                                               │             │
│   │ - IPermissionDal                                   │             │
│   │ - IRolePermissionDal                               │             │
│   └────────────────────────────────────────────────────┘             │
│                                                                       │
└───────────────────────────┬───────────────────────────────────────────┘
                            │
┌───────────────────────────┼───────────────────────────────────────────┐
│              DATABASE SCHEMA (Updated)                                │
├───────────────────────────┼───────────────────────────────────────────┤
│                           ▼                                           │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ Users Table                                             │       │
│   ├─────────────────────────────────────────────────────────┤       │
│   │ Id, Name, Email, Password, RoleId (int), ...           │       │
│   └──────────────────────────┬──────────────────────────────┘       │
│                              │                                       │
│                              │ Foreign Key                           │
│                              ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ Roles Table                                             │       │
│   ├─────────────────────────────────────────────────────────┤       │
│   │ Id, Name, Description, IsSystem, IsActive, ...          │       │
│   │                                                         │       │
│   │ Examples:                                               │       │
│   │ (1, "User", "Basic user", 1, 1)                         │       │
│   │ (2, "Admin", "Administrator", 1, 1)                     │       │
│   │ (3, "Fleet Manager", "Manages fleet", 0, 1)  ← Dynamic! │       │
│   │ (4, "Maintenance Tech", "Handles maint", 0, 1)          │       │
│   │ (5, "Purchase Manager", "Handles purchases", 0, 1)      │       │
│   └──────────────────────────┬──────────────────────────────┘       │
│                              │                                       │
│                              │ Junction Table                        │
│                              ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ RolePermissions Table  ← NEW!                           │       │
│   ├─────────────────────────────────────────────────────────┤       │
│   │ RoleId, PermissionId, GrantedAt, GrantedBy              │       │
│   │                                                         │       │
│   │ Examples:                                               │       │
│   │ (3, 1, ...)  ← Fleet Manager has ReadVehicles          │       │
│   │ (3, 2, ...)  ← Fleet Manager has WriteVehicles         │       │
│   │ (4, 1, ...)  ← Maintenance Tech has ReadVehicles       │       │
│   │ (4, 5, ...)  ← Maintenance Tech has WriteMaintenance   │       │
│   └──────────────────────────┬──────────────────────────────┘       │
│                              │                                       │
│                              │ Foreign Key                           │
│                              ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ Permissions Table  ← NEW!                               │       │
│   ├─────────────────────────────────────────────────────────┤       │
│   │ Id, Name, DisplayName, Description, Module, ...         │       │
│   │                                                         │       │
│   │ Examples (grouped by module):                           │       │
│   │                                                         │       │
│   │ Vehicles Module:                                        │       │
│   │ (1, "ReadVehicles", "View Vehicles", ..., "Vehicles")   │       │
│   │ (2, "WriteVehicles", "Edit Vehicles", ..., "Vehicles")  │       │
│   │ (3, "DeleteVehicles", "Delete Vehicles", ..., "Vehicles")│      │
│   │                                                         │       │
│   │ Maintenance Module:                                     │       │
│   │ (4, "ReadMaintenance", "View Maintenance", ..., "Maint")│       │
│   │ (5, "WriteMaintenance", "Edit Maintenance", ..., "Maint")│      │
│   │                                                         │       │
│   │ Fleet Module:                                           │       │
│   │ (6, "AssignVehicles", "Assign Vehicles", ..., "Fleet")  │       │
│   │ (7, "ManageRoutes", "Manage Routes", ..., "Fleet")      │       │
│   │                                                         │       │
│   │ Purchase Module:                                        │       │
│   │ (8, "ReadPurchases", "View Purchases", ..., "Purchase") │       │
│   │ (9, "WritePurchases", "Manage Purchases", ..., "Purchase")│     │
│   │ (10, "ApprovePurchases", "Approve Purchase", ..., "Purchase")│  │
│   │                                                         │       │
│   │ Admin Module:                                           │       │
│   │ (11, "ManageUsers", "Manage Users", ..., "Admin")       │       │
│   │ (12, "ManageRoles", "Manage Roles", ..., "Admin")       │       │
│   │ (13, "ViewLogs", "View System Logs", ..., "Admin")      │       │
│   └─────────────────────────────────────────────────────────┘       │
└───────────────────────────────────────────────────────────────────────┘

AUTHORIZATION FLOW EXAMPLE:

User clicks "Edit Vehicle" button
    │
    ▼
VehicleEditViewModel.EditCommandAsync()
    │
    │ await _authService.HasPermissionAsync("WriteVehicles");
    │
    ▼
AuthorizationService.HasPermissionAsync("WriteVehicles")
    │
    │ 1. Get current user's RoleId from CurrentUserService
    │ 2. Check cache first
    │ 3. Query RolePermissions table
    │
    ▼
RolePermissionDal.RoleHasPermissionAsync(roleId, "WriteVehicles")
    │
    │ SELECT COUNT(*) FROM RolePermissions rp
    │ JOIN Permissions p ON rp.PermissionId = p.Id
    │ WHERE rp.RoleId = @roleId AND p.Name = @permissionName
    │
    ▼
Returns: true/false
    │
    ▼
If false: Show error "You don't have permission to edit vehicles"
If true: Proceed with edit operation

IMPROVEMENTS:
✓ UserRole enum removed
✓ Fully dynamic role creation (no recompilation)
✓ Granular permission-based authorization
✓ Admin can configure roles/permissions through UI
✓ Perfect for multi-tenant enterprise apps
✓ Permissions organized by module
✓ Flexible enough for any business scenario
```

---

## After Iteration 4: Fleet Management Core Added

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER (Expanded)                    │
│                      myFlatLightLogin.UI.Wpf                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Authentication        Fleet Management            Admin               │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐        │
│  │  LoginView   │      │ VehicleList  │      │  RolesMgmt   │        │
│  │ RegisterView │      │ VehicleEdit  │      │  UsersMgmt   │        │
│  │ ProfileView  │      │  DriverList  │      │  LogsView    │        │
│  └──────────────┘      │  DriverEdit  │      └──────────────┘        │
│                        │ AssignmentView│                              │
│                        │ FleetDashboard│                              │
│                        └──────────────┘                              │
│                                                                       │
└───────────────────────────────┬───────────────────────────────────────┘
                                │
┌───────────────────────────────┼───────────────────────────────────────┐
│              BUSINESS LOGIC LAYER (Expanded)                          │
│              myFlatLightLogin.Library                                │
├───────────────────────────────┼───────────────────────────────────────┤
│                               ▼                                       │
│  ┌────────────────────────────────────────────────────────┐         │
│  │                    Business Services                   │         │
│  ├────────────────────────────────────────────────────────┤         │
│  │ Authentication:           Fleet Management:            │         │
│  │ - UserService             - VehicleService             │         │
│  │ - RoleService             - DriverService              │         │
│  │ - AuthService             - AssignmentService          │         │
│  │ - AuthorizationService    - FuelLogService             │         │
│  │                           - IncidentService            │         │
│  └────────────────────────────────────────────────────────┘         │
│                                                                      │
│  ┌────────────────────────────────────────────────────────┐         │
│  │                  Business Objects                      │         │
│  ├────────────────────────────────────────────────────────┤         │
│  │ Authentication:           Fleet Management:            │         │
│  │ - User                    - Vehicle                    │         │
│  │ - Role                    - Driver                     │         │
│  │ - Permission              - Assignment                 │         │
│  │                           - FuelLog                    │         │
│  │                           - Incident                   │         │
│  │                           - Location                   │         │
│  └────────────────────────────────────────────────────────┘         │
│                                                                      │
│  ┌────────────────────────────────────────────────────────┐         │
│  │                   Business Rules                       │         │
│  ├────────────────────────────────────────────────────────┤         │
│  │ Vehicle Rules:                                         │         │
│  │ - UniqueVINRule                                        │         │
│  │ - ValidLicensePlateRule                                │         │
│  │ - VehicleStatusRule                                    │         │
│  │                                                        │         │
│  │ Driver Rules:                                          │         │
│  │ - ValidLicenseNumberRule                               │         │
│  │ - LicenseNotExpiredRule                                │         │
│  │ - MinimumAgeRule                                       │         │
│  │                                                        │         │
│  │ Assignment Rules:                                      │         │
│  │ - NoDoubleBookingRule                                  │         │
│  │ - DriverLicenseValidRule                               │         │
│  │ - VehicleAvailableRule                                 │         │
│  └────────────────────────────────────────────────────────┘         │
│                                                                      │
└──────────────────────────────┬───────────────────────────────────────┘
                               │
┌──────────────────────────────┼───────────────────────────────────────┐
│           DATA ACCESS LAYER (Expanded)                               │
│           myFlatLightLogin.Dal + Implementations                    │
├──────────────────────────────┼───────────────────────────────────────┤
│                              ▼                                       │
│  ┌────────────────────────────────────────────────────────┐         │
│  │                   DAL Interfaces                       │         │
│  ├────────────────────────────────────────────────────────┤         │
│  │ Authentication:           Fleet Management:            │         │
│  │ - IUserDal                - IVehicleDal                │         │
│  │ - IRoleDal                - IDriverDal                 │         │
│  │ - IPermissionDal          - IAssignmentDal             │         │
│  │ - IRolePermissionDal      - IFuelLogDal                │         │
│  │                           - IIncidentDal               │         │
│  └────────────────────────────────────────────────────────┘         │
│                                                                      │
│  Each interface has:                                                │
│  - SQLite implementation (offline-first)                             │
│  - Firebase implementation (cloud sync)                              │
│                                                                      │
└──────────────────────────────┬───────────────────────────────────────┘
                               │
┌──────────────────────────────┼───────────────────────────────────────┐
│              DATABASE SCHEMA (Expanded)                              │
├──────────────────────────────┼───────────────────────────────────────┤
│                              ▼                                       │
│  EXISTING TABLES:                                                    │
│  - Users                                                             │
│  - Roles                                                             │
│  - Permissions                                                       │
│  - RolePermissions                                                   │
│                                                                      │
│  NEW TABLES:                                                         │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────┐        │
│  │ Vehicles                                                │        │
│  ├─────────────────────────────────────────────────────────┤        │
│  │ Id, Make, Model, Year, VIN, LicensePlate, Color,        │        │
│  │ Status, Odometer, FuelType, Capacity, PurchaseDate,     │        │
│  │ PurchasePrice, CurrentValue, LastServiceDate,           │        │
│  │ NextServiceDate, CreatedAt, UpdatedAt, NeedsSync        │        │
│  └─────────────────────────────────────────────────────────┘        │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────┐        │
│  │ Drivers                                                 │        │
│  ├─────────────────────────────────────────────────────────┤        │
│  │ Id, Name, LicenseNumber, LicenseExpiry, DateOfBirth,    │        │
│  │ Phone, Email, Address, HireDate, Status,                │        │
│  │ CreatedAt, UpdatedAt, NeedsSync                         │        │
│  └─────────────────────────────────────────────────────────┘        │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────┐        │
│  │ Assignments                                             │        │
│  ├─────────────────────────────────────────────────────────┤        │
│  │ Id, VehicleId, DriverId, StartDate, EndDate,            │        │
│  │ Status, Notes, CreatedAt, UpdatedAt, NeedsSync          │        │
│  └─────────────────────────────────────────────────────────┘        │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────┐        │
│  │ FuelLogs                                                │        │
│  ├─────────────────────────────────────────────────────────┤        │
│  │ Id, VehicleId, DriverId, Date, Odometer, Liters,        │        │
│  │ CostPerLiter, TotalCost, FuelType, Location,            │        │
│  │ CreatedAt, NeedsSync                                    │        │
│  └─────────────────────────────────────────────────────────┘        │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────┐        │
│  │ Incidents                                               │        │
│  ├─────────────────────────────────────────────────────────┤        │
│  │ Id, VehicleId, DriverId, Date, Type, Severity,          │        │
│  │ Description, Location, Cost, Status, ReportedBy,        │        │
│  │ CreatedAt, UpdatedAt, NeedsSync                         │        │
│  └─────────────────────────────────────────────────────────┘        │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘

KEY FEATURES IN ITERATION 4:

1. Vehicle Management
   ✓ Add/Edit/Delete vehicles
   ✓ Track vehicle details (VIN, license, odometer, etc.)
   ✓ Vehicle status tracking (Available, Assigned, Maintenance, Retired)
   ✓ Search and filter vehicles

2. Driver Management
   ✓ Add/Edit/Delete drivers
   ✓ Track license information and expiry
   ✓ Driver status (Active, Inactive, On Leave, Terminated)
   ✓ License validation rules

3. Assignment Management
   ✓ Assign drivers to vehicles
   ✓ Track assignment periods
   ✓ Prevent double-booking (business rule)
   ✓ View current and historical assignments

4. Basic Tracking
   ✓ Fuel log entries
   ✓ Incident reports
   ✓ Basic cost tracking

5. Reporting
   ✓ Fleet overview dashboard
   ✓ Vehicles by status
   ✓ Driver assignments
   ✓ Fuel consumption summary
   ✓ Incident summary

ALL WITH:
✓ Offline-first architecture (works without internet)
✓ Automatic sync with Firebase when online
✓ Permission-based access control
✓ Business rule validation
✓ CSLA-style factory methods
```

---

## Iteration 5+: Domain Modules

As additional modules are added (Maintenance, Purchase, Dispatch, etc.), the architecture follows the same pattern:

**For each new module:**

1. **Database Layer:**
   - Add new tables (e.g., MaintenancePlans, PurchaseOrders)
   - Define permissions (e.g., ReadMaintenance, WritePurchaseOrders)

2. **DAL Layer:**
   - Create interface (e.g., IMaintenanceDal)
   - Implement SQLite version
   - Implement Firebase version

3. **BLL Layer:**
   - Create business object (e.g., MaintenancePlan)
   - Create factory (e.g., MaintenancePlanFactory)
   - Create service (e.g., IMaintenanceService)
   - Add business rules (e.g., ValidServiceIntervalRule)

4. **UI Layer:**
   - Create ViewModels (e.g., MaintenanceListViewModel)
   - Create Views (XAML)
   - Add permission checks (e.g., _authService.HasPermissionAsync("WriteMaintenance"))

**The architecture scales horizontally** - each module is independent but follows the same layered structure.

---

## Dependency Injection Container

As the application grows, the DI container registration becomes more important:

```csharp
// Program.cs or App.xaml.cs

services.AddSingleton<INetworkConnectivityService, NetworkConnectivityService>();
services.AddSingleton<ICurrentUserService, CurrentUserService>();

// DAL - SQLite
services.AddScoped<IUserDal, myFlatLightLogin.DalSQLite.UserDal>();
services.AddScoped<IRoleDal, myFlatLightLogin.DalSQLite.RoleDal>();
services.AddScoped<IPermissionDal, myFlatLightLogin.DalSQLite.PermissionDal>();
services.AddScoped<IRolePermissionDal, myFlatLightLogin.DalSQLite.RolePermissionDal>();
services.AddScoped<IVehicleDal, myFlatLightLogin.DalSQLite.VehicleDal>();
services.AddScoped<IDriverDal, myFlatLightLogin.DalSQLite.DriverDal>();
// ... more DALs

// BLL - Services
services.AddScoped<IUserService, UserService>();
services.AddScoped<IRoleService, RoleService>();
services.AddScoped<IAuthorizationService, AuthorizationService>();
services.AddScoped<IVehicleService, VehicleService>();
services.AddScoped<IDriverService, DriverService>();
services.AddScoped<IAssignmentService, AssignmentService>();
// ... more services

// ViewModels
services.AddTransient<LoginViewModel>();
services.AddTransient<RegisterViewModel>();
services.AddTransient<VehicleListViewModel>();
services.AddTransient<VehicleEditViewModel>();
services.AddTransient<DriverListViewModel>();
services.AddTransient<RoleManagementViewModel>();
// ... more ViewModels
```

---

## Summary of Architectural Principles

Throughout all iterations, these principles remain constant:

1. **Separation of Concerns**
   - UI knows nothing about database
   - Business logic knows nothing about UI
   - DAL knows nothing about business rules

2. **Dependency Inversion**
   - Depend on abstractions (interfaces), not concrete implementations
   - BLL depends on IUserDal, not UserDal (SQLite)
   - ViewModels depend on IUserService, not UserService

3. **Single Responsibility**
   - ViewModels: UI state and commands only
   - Services: Orchestrate business operations
   - Business Objects: Encapsulate entity behavior
   - DAL: Pure data access

4. **Open/Closed Principle**
   - Open for extension (add new modules easily)
   - Closed for modification (existing code doesn't change)

5. **Don't Repeat Yourself (DRY)**
   - Business rules defined once (in BLL)
   - Reused across all services and objects

6. **Testability**
   - All layers mockable
   - Business rules tested independently
   - Services tested with mocked DAL

---

This architecture provides a solid foundation that scales from a simple authentication app to a comprehensive, enterprise-ready fleet management platform.
