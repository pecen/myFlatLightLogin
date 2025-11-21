import Foundation
import SQLite

/// Database manager for SQLite operations
class DatabaseManager {
    static let shared = DatabaseManager()

    private var db: Connection?

    // User table definition
    private let users = Table("users")
    private let id = Expression<Int64>("id")
    private let name = Expression<String>("name")
    private let lastname = Expression<String>("lastname")
    private let username = Expression<String>("username")
    private let password = Expression<String>("password")

    private init() {
        do {
            let path = getDatabasePath()
            db = try Connection(path)
            createTables()
        } catch {
            print("Database connection error: \(error)")
        }
    }

    private func getDatabasePath() -> String {
        let fileManager = FileManager.default
        let appSupportURL = fileManager.urls(for: .applicationSupportDirectory, in: .userDomainMask).first!
        let appDirectory = appSupportURL.appendingPathComponent("myFlatLightLogin")

        // Create directory if it doesn't exist
        if !fileManager.fileExists(atPath: appDirectory.path) {
            try? fileManager.createDirectory(at: appDirectory, withIntermediateDirectories: true)
        }

        return appDirectory.appendingPathComponent("security.db3").path
    }

    private func createTables() {
        do {
            try db?.run(users.create(ifNotExists: true) { table in
                table.column(id, primaryKey: .autoincrement)
                table.column(name)
                table.column(lastname)
                table.column(username)
                table.column(password)
            })
        } catch {
            print("Table creation error: \(error)")
        }
    }

    // MARK: - User Operations

    /// Fetch user by ID
    func fetchUser(byId userId: Int64) -> User? {
        guard let db = db else { return nil }

        do {
            let query = users.filter(id == userId)
            if let row = try db.pluck(query) {
                return User(
                    id: row[id],
                    name: row[name],
                    lastname: row[lastname],
                    username: row[username],
                    password: row[password]
                )
            }
        } catch {
            print("Fetch user error: \(error)")
        }
        return nil
    }

    /// Fetch user by username
    func fetchUser(byUsername userName: String) -> User? {
        guard let db = db else { return nil }

        do {
            let query = users.filter(username == userName)
            if let row = try db.pluck(query) {
                return User(
                    id: row[id],
                    name: row[name],
                    lastname: row[lastname],
                    username: row[username],
                    password: row[password]
                )
            }
        } catch {
            print("Fetch user error: \(error)")
        }
        return nil
    }

    /// Insert a new user
    func insertUser(_ user: User) -> Bool {
        guard let db = db else { return false }

        do {
            let insert = users.insert(
                name <- user.name,
                lastname <- user.lastname,
                username <- user.username,
                password <- user.password
            )
            try db.run(insert)
            return true
        } catch {
            print("Insert user error: \(error)")
            return false
        }
    }

    /// Update an existing user
    func updateUser(_ user: User) -> Bool {
        guard let db = db, let userId = user.id else { return false }

        do {
            let userRow = users.filter(id == userId)
            try db.run(userRow.update(
                name <- user.name,
                lastname <- user.lastname,
                username <- user.username,
                password <- user.password
            ))
            return true
        } catch {
            print("Update user error: \(error)")
            return false
        }
    }

    /// Delete a user by ID
    func deleteUser(byId userId: Int64) -> Bool {
        guard let db = db else { return false }

        do {
            let userRow = users.filter(id == userId)
            try db.run(userRow.delete())
            return true
        } catch {
            print("Delete user error: \(error)")
            return false
        }
    }

    /// Fetch all users
    func fetchAllUsers() -> [User] {
        guard let db = db else { return [] }

        var result: [User] = []
        do {
            for row in try db.prepare(users) {
                let user = User(
                    id: row[id],
                    name: row[name],
                    lastname: row[lastname],
                    username: row[username],
                    password: row[password]
                )
                result.append(user)
            }
        } catch {
            print("Fetch all users error: \(error)")
        }
        return result
    }
}
