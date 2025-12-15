# Hybrid Initialization Pattern

This document describes the hybrid initialization pattern used for role data access in the myFlatLightLogin application.

## Overview

The hybrid approach combines **eager initialization** at application startup with **lazy initialization** as a fallback. This provides the benefits of both patterns while mitigating their drawbacks.

## Why Hybrid?

### Eager Initialization Only

| Pros | Cons |
|------|------|
| Fail-fast on startup | Must remember to call it |
| Predictable timing | If forgotten, no initialization happens |
| Can show progress to user | Adds to startup time |

### Lazy Initialization Only

| Pros | Cons |
|------|------|
| Self-contained DAL | First-access latency |
| Guaranteed initialization | Unpredictable timing |
| Simpler calling code | Errors surface late |

### Hybrid Approach

| Benefit | Source |
|---------|--------|
| Fail-fast on startup | Eager |
| Guaranteed initialization | Lazy fallback |
| No first-access latency (if eager succeeds) | Eager |
| Resilient to forgotten calls | Lazy fallback |

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        UI Layer                                  │
│                     (App.xaml.cs)                                │
│                                                                  │
│   Log: "Initializing Roles..."                                   │
│   await _hybridRoleDal.InitializeAsync()  ← Eager call           │
│   Log: "Roles initialized successfully"                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   BLL/Core Layer                                 │
│                  (HybridRoleDal)                                 │
│                                                                  │
│   Log: "Initializing role providers..."                          │
│   await _sqliteDal.InitializeAsync()                             │
│   if (online) await firebaseDal.InitializeAsync()                │
│   Log: "Role providers initialized successfully"                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                    ┌─────────┴─────────┐
                    ▼                   ▼
┌───────────────────────────┐ ┌───────────────────────────┐
│      SQLite DAL           │ │      Firebase DAL         │
│   (DalSQLite.RoleDal)     │ │  (DalFirebase.RoleDal)    │
│                           │ │                           │
│ Log: "Initializing SQLite │ │ Log: "Initializing        │
│       roles database..."  │ │       Firebase Realtime   │
│                           │ │       Database roles..."  │
│ - Creates table if needed │ │ - Uses Lazy<Task>         │
│ - Seeds default roles     │ │ - Seeds default roles     │
│                           │ │                           │
│ Log: "SQLite roles        │ │ Log: "Firebase roles      │
│       initialized"        │ │       initialized"        │
└───────────────────────────┘ └───────────────────────────┘
```

## Implementation Details

### 1. Interface (IRoleDal)

The interface defines `InitializeAsync()` so all implementations must provide it:

```csharp
public interface IRoleDal
{
    /// <summary>
    /// Initializes the role data store asynchronously.
    /// Seeds default roles if they don't exist.
    /// </summary>
    Task InitializeAsync();

    // ... other methods
}
```

### 2. Firebase DAL - Lazy<Task> Pattern

The Firebase implementation uses `Lazy<Task>` for thread-safe lazy initialization:

```csharp
public class RoleDal : IRoleDal
{
    private readonly Lazy<Task> _initializationTask;

    public RoleDal(string authToken = null)
    {
        // ... Firebase client setup ...

        // Lazy initialization with thread safety
        _initializationTask = new Lazy<Task>(
            InitializeRolesAsync,
            LazyThreadSafetyMode.ExecutionAndPublication
        );
    }

    // For eager initialization at startup
    public async Task InitializeAsync()
    {
        _logger.Information("Initializing Firebase Realtime Database roles...");
        await EnsureInitializedAsync();
        _logger.Information("Firebase Realtime Database roles initialized successfully");
    }

    // Called by all DAL methods - triggers lazy init if not done
    private Task EnsureInitializedAsync()
    {
        return _initializationTask.Value;
    }

    private async Task InitializeRolesAsync()
    {
        // Actual initialization work
        // Check for existing roles, seed defaults if needed
    }
}
```

#### How Lazy<Task> Works

1. **First access**: When `_initializationTask.Value` is accessed, `InitializeRolesAsync()` is invoked
2. **Subsequent accesses**: The same `Task` is returned (already completed or in progress)
3. **Thread safety**: `ExecutionAndPublication` mode ensures only one thread starts the initialization

### 3. SQLite DAL

SQLite initializes synchronously in the constructor but provides `InitializeAsync()` for interface compliance:

```csharp
public class RoleDal : IRoleDal
{
    public RoleDal()
    {
        _dbPath = Path.Combine(Environment.CurrentDirectory, "security.db3");
        InitializeDatabase(); // Synchronous initialization
    }

    public Task InitializeAsync()
    {
        _logger.Information("Initializing SQLite roles database at {DbPath}...", _dbPath);

        using (var conn = new SQLiteConnection(_dbPath))
        {
            var roleCount = conn.Table<Role>().Count();
            _logger.Information("SQLite roles initialized successfully. Found {RoleCount} roles", roleCount);
        }

        return Task.CompletedTask;
    }
}
```

### 4. HybridRoleDal (BLL/Core Layer)

Orchestrates initialization of both data stores:

```csharp
public class HybridRoleDal : IRoleDal
{
    public async Task InitializeAsync()
    {
        _logger.Information("Initializing role providers...");

        // Always initialize SQLite first (local cache)
        await _sqliteDal.InitializeAsync();

        // If online, also initialize Firebase
        if (_connectivityService.IsOnline)
        {
            try
            {
                _logger.Debug("Online - initializing remote role provider...");
                var firebaseDal = GetFirebaseDal();
                await firebaseDal.InitializeAsync();
            }
            catch (Exception ex)
            {
                // Firebase failed but SQLite succeeded - app can work offline
                _logger.Warning("Remote role provider initialization failed: {ErrorMessage}", ex.Message);
            }
        }

        _logger.Information("Role providers initialized successfully");
    }
}
```

### 5. UI Layer (App.xaml.cs)

Calls initialization at startup with generic logging:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    // Initialize Roles asynchronously (seeds default roles if they don't exist)
    _ = Task.Run(async () =>
    {
        try
        {
            Log.Information("Initializing Roles...");
            await _hybridRoleDal.InitializeAsync();
            Log.Information("Roles initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to initialize Roles: {ErrorMessage}", ex.Message);
        }
    });

    // ... rest of startup
}
```

## Logging Hierarchy

Each layer logs at its appropriate level of abstraction:

| Layer | Log Messages |
|-------|--------------|
| **UI** | "Initializing Roles...", "Roles initialized successfully" |
| **HybridRoleDal** | "Initializing role providers...", "Role providers initialized successfully" |
| **Firebase DAL** | "Initializing Firebase Realtime Database roles...", "Checking Firebase for existing roles..." |
| **SQLite DAL** | "Initializing SQLite roles database at {path}...", "Creating SQLite roles table..." |

This follows the **Dependency Inversion Principle**: the UI doesn't know whether roles come from Firebase, SQLite, or any other source.

## Scenario Walkthrough

### Scenario 1: Normal Startup (Online)

```
1. App.xaml.cs: "Initializing Roles..."
2. HybridRoleDal: "Initializing role providers..."
3. SQLite RoleDal: "Initializing SQLite roles database..."
4. SQLite RoleDal: "SQLite roles initialized successfully. Found 3 roles"
5. HybridRoleDal: "Online - initializing remote role provider..."
6. Firebase RoleDal: "Initializing Firebase Realtime Database roles..."
7. Firebase RoleDal: "Found 2 existing roles in Firebase"
8. Firebase RoleDal: "Firebase Realtime Database roles initialized successfully"
9. HybridRoleDal: "Role providers initialized successfully"
10. App.xaml.cs: "Roles initialized successfully"
```

### Scenario 2: Startup Offline

```
1. App.xaml.cs: "Initializing Roles..."
2. HybridRoleDal: "Initializing role providers..."
3. SQLite RoleDal: "Initializing SQLite roles database..."
4. SQLite RoleDal: "SQLite roles initialized successfully. Found 3 roles"
5. HybridRoleDal: "Offline - skipping remote role provider initialization"
6. HybridRoleDal: "Role providers initialized successfully"
7. App.xaml.cs: "Roles initialized successfully"
```

### Scenario 3: Lazy Fallback (Startup Skipped)

If the startup initialization was somehow skipped:

```
1. User calls RoleDal.Fetch("Admin")
2. GetRoleByNameAsync calls EnsureInitializedAsync()
3. Lazy<Task>.Value triggers InitializeRolesAsync()
4. Firebase RoleDal: "Checking Firebase for existing roles..."
5. Initialization completes
6. Fetch continues and returns the role
```

## Benefits Summary

1. **Fail-fast**: Configuration issues are detected at startup
2. **Resilient**: Even if startup fails, operations still work via lazy init
3. **Thread-safe**: `Lazy<Task>` with `ExecutionAndPublication` prevents race conditions
4. **Decoupled**: UI only knows about generic "Roles", not Firebase/SQLite specifics
5. **Predictable**: Startup initialization happens at a known time
6. **Efficient**: Initialization only happens once, regardless of how many callers
