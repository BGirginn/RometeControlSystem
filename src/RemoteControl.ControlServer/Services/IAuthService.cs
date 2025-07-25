namespace RemoteControl.ControlServer.Services;

/// <summary>
/// Authentication service for user management
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user
    /// </summary>
    Task<AuthResult> RegisterAsync(string username, string password, string email);

    /// <summary>
    /// Login a user
    /// </summary>
    Task<AuthResult> LoginAsync(string username, string password);

    /// <summary>
    /// Validate a JWT token
    /// </summary>
    Task<AuthResult> ValidateTokenAsync(string token);

    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<UserInfo?> GetUserAsync(string userId);

    /// <summary>
    /// Update user last login
    /// </summary>
    Task UpdateUserLoginAsync(string userId);
}

/// <summary>
/// User information
/// </summary>
public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Authentication result
/// </summary>
public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? Message { get; set; }
    public UserInfo? User { get; set; }
} 