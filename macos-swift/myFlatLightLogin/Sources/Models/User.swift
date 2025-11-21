import Foundation

/// Database entity model for User
struct User: Identifiable, Codable {
    var id: Int64?
    var name: String
    var lastname: String
    var username: String
    var password: String

    init(id: Int64? = nil, name: String = "", lastname: String = "", username: String = "", password: String = "") {
        self.id = id
        self.name = name
        self.lastname = lastname
        self.username = username
        self.password = password
    }
}

/// Data Transfer Object for User
struct UserDto {
    var id: Int64?
    var name: String
    var lastname: String
    var username: String
    var password: String
    var confirmPassword: String

    init(id: Int64? = nil, name: String = "", lastname: String = "", username: String = "", password: String = "", confirmPassword: String = "") {
        self.id = id
        self.name = name
        self.lastname = lastname
        self.username = username
        self.password = password
        self.confirmPassword = confirmPassword
    }

    /// Convert DTO to database entity
    func toUser() -> User {
        User(id: id, name: name, lastname: lastname, username: username, password: password)
    }

    /// Create DTO from database entity
    static func from(_ user: User) -> UserDto {
        UserDto(id: user.id, name: user.name, lastname: user.lastname, username: user.username, password: user.password)
    }
}
