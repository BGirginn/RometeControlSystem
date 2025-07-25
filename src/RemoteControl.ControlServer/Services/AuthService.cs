using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using RemoteControl.Transport.Authentication;

namespace RemoteControl.ControlServer.Services;

/// <summary>
/// In-memory authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IJwtService _jwtService;
    private readonly ConcurrentDictionary<string, UserInfo> _users = new();
    private readonly ConcurrentDictionary<string, string> _usernameToIdMap = new();

    public AuthService(ILogger<AuthService> logger, IJwtService jwtService)
    {
        _logger = logger;
        _jwtService = jwtService;
        SeedDefaultUsers();
    }

    public async Task<AuthResult> RegisterAsync(string username, string password, string email)
    {
        try
        {
            // Check if username already exists
            if (_usernameToIdMap.ContainsKey(username.ToLower()))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Username already exists"
                };
            }

            // Check password strength
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Password must be at least 6 characters long"
                };
            }

            var userId = Guid.NewGuid().ToString();
            var passwordHash = HashPassword(password);

            var user = new UserInfo
            {
                UserId = userId,
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true
            };

            _users[userId] = user;
            _usernameToIdMap[username.ToLower()] = userId;

            _logger.LogInformation("User registered: {Username} ({UserId})", username, userId);

            return new AuthResult
            {
                Success = true,
                UserId = userId,
                Username = username,
                Message = "Registration successful",
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration: {Username}", username);
            return new AuthResult
            {
                Success = false,
                Message = $"Registration failed: {ex.Message}"
            };
        }
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            if (!_usernameToIdMap.TryGetValue(username.ToLower(), out var userId) ||
                !_users.TryGetValue(userId, out var user))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            if (!user.IsActive)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Account is disabled"
                };
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;

            // Generate JWT token
            var token = _jwtService.GenerateToken(userId, username);

            _logger.LogInformation("User logged in: {Username} ({UserId})", username, userId);

            return new AuthResult
            {
                Success = true,
                Token = token,
                UserId = userId,
                Username = username,
                Message = "Login successful",
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login: {Username}", username);
            return new AuthResult
            {
                Success = false,
                Message = $"Login failed: {ex.Message}"
            };
        }
    }

    public async Task<AuthResult> ValidateTokenAsync(string token)
    {
        try
        {
            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            var userId = _jwtService.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userId) || !_users.TryGetValue(userId, out var user))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            if (!user.IsActive)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Account is disabled"
                };
            }

            if (_jwtService.IsTokenExpired(token))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Token expired"
                };
            }

            return new AuthResult
            {
                Success = true,
                UserId = userId,
                Username = user.Username,
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return new AuthResult
            {
                Success = false,
                Message = $"Token validation failed: {ex.Message}"
            };
        }
    }

    public async Task<UserInfo?> GetUserAsync(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return await Task.FromResult(user);
    }

    public async Task UpdateUserLoginAsync(string userId)
    {
        if (_users.TryGetValue(userId, out var user))
        {
            user.LastLoginAt = DateTime.UtcNow;
        }

        await Task.CompletedTask;
    }

    private void SeedDefaultUsers()
    {
        // Create default demo user
        var demoUserId = Guid.NewGuid().ToString();
        var demoUser = new UserInfo
        {
            UserId = demoUserId,
            Username = "demo",
            Email = "demo@example.com",
            PasswordHash = HashPassword("demo123"),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true
        };

        _users[demoUserId] = demoUser;
        _usernameToIdMap["demo"] = demoUserId;

        _logger.LogInformation("Demo user created: demo/demo123");
    }

    private static string HashPassword(string password)
    {
        // Simple demo hashing - in production, use BCrypt or Argon2
        using var sha256 = SHA256.Create();
        var saltedPassword = $"salt_{password}_more_salt";
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
} 