# Firebase Security Rules for Role Management

Your Firebase Realtime Database security rules are currently blocking access to the `/roles` path, even for authenticated users.

## Problem

The error shows:
```
Response: {
  "error" : "Permission denied"
}
```

This means your Firebase security rules need to be updated to allow authenticated users to access roles.

## Solution

### Option 1: Allow All Authenticated Users to Read Roles (Recommended)

Update your Firebase Realtime Database rules to:

```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "roles": {
      ".read": "auth != null",
      ".write": "auth != null"
    }
  }
}
```

This allows:
- **Users**: Each user can only read/write their own data
- **Roles**: All authenticated users can read/write roles

### Option 2: Admin-Only Role Access (More Secure)

If you want only admins to manage roles:

```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "roles": {
      ".read": "auth != null && root.child('users').child(auth.uid).child('Role').val() === 1",
      ".write": "auth != null && root.child('users').child(auth.uid).child('Role').val() === 1"
    }
  }
}
```

This requires:
- User must be authenticated (`auth != null`)
- User must have `Role` field set to `1` (Admin role)

### Option 3: Public Read, Admin Write (Hybrid)

If you want everyone to see roles, but only admins to modify them:

```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "roles": {
      ".read": "auth != null",
      ".write": "auth != null && root.child('users').child(auth.uid).child('Role').val() === 1"
    }
  }
}
```

## How to Update Firebase Security Rules

1. Go to **Firebase Console**: https://console.firebase.google.com/
2. Select your project: **myFlatLightLogin**
3. Click **Realtime Database** in the left menu
4. Click the **Rules** tab
5. Replace the existing rules with one of the options above
6. Click **Publish**

## Current State

Your current rules likely look like this:

```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    }
  }
}
```

Notice there's **no `roles` section** - that's why access is denied!

## Testing After Update

After updating the rules:

1. Restart your application
2. Sign in as an admin user
3. Click "Manage Roles (Admin)"
4. Click "Seed Default Roles"
5. The roles should now be created successfully!

## Recommended Choice

I recommend **Option 1** for development/testing, then move to **Option 2** for production to restrict role management to admins only.

## Notes

- The `Role` field in your user profile must be an integer: `0` = User, `1` = Admin
- Make sure your admin user has `Role: 1` set in their Firebase user profile
- Firebase rules are case-sensitive
