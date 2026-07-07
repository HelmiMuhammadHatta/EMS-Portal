using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EMS.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IApplicationDbContext context, IConfiguration config, ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new Exception("Email already exists.");

        var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Staff") 
            ?? throw new Exception("Default role not found.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = staffRole.Id,
            IsActive = true,
            EmailVerified = true, // Auto verified for demo
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"[MOCK EMAIL] Verification email sent to {request.Email}");
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            throw new Exception("Invalid email or password.");

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            throw new Exception($"Account is locked out until {user.LockoutEnd.Value.ToLocalTime()}.");

        bool isPasswordValid = false;
        
        // Handle unhashed seeded passwords (like 'Admin123!')
        if (!user.PasswordHash.StartsWith("$2"))
        {
            if (user.PasswordHash == request.Password)
            {
                isPasswordValid = true;
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password); // Auto-migrate to BCrypt
            }
        }
        else
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        }

        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginAttempts = 0;
            }
            await _context.SaveChangesAsync();
            throw new Exception("Invalid email or password.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        var tokenResponse = GenerateTokens(user);
        user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(tokenResponse.RefreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return tokenResponse;
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null) throw new Exception("Invalid access token.");

        var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            throw new Exception("Invalid token claims.");

        var user = await _context.Users
            .Include(u => u.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow || string.IsNullOrEmpty(user.RefreshTokenHash))
            throw new Exception("Invalid or expired refresh token.");

        if (!BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash))
            throw new Exception("Invalid refresh token.");

        var tokenResponse = GenerateTokens(user);
        user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(tokenResponse.RefreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();
        return tokenResponse;
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiry = null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return;

        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.ResetTokenHash = BCrypt.Net.BCrypt.HashPassword(resetToken);
        user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

        await _context.SaveChangesAsync();
        _logger.LogInformation($"[MOCK EMAIL] Password reset token for {request.Email}: {resetToken}");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || string.IsNullOrEmpty(user.ResetTokenHash) || user.ResetTokenExpiry < DateTime.UtcNow)
            throw new Exception("Invalid or expired reset token.");

        if (!BCrypt.Net.BCrypt.Verify(request.Token, user.ResetTokenHash))
            throw new Exception("Invalid reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetTokenHash = null;
        user.ResetTokenExpiry = null;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        await _context.SaveChangesAsync();
    }

    public async Task<object> GetCurrentUserAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) throw new Exception("User not found.");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

        return new
        {
            user.Id,
            user.Email,
            EmployeeId = employee?.Id,
            Role = user.Role.Name,
            Permissions = user.Role.RolePermissions.Select(rp => rp.Permission.Name).ToList()
        };
    }

    private TokenResponse GenerateTokens(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var employee = _context.Employees.FirstOrDefault(e => e.UserId == user.Id);
        if (employee != null)
        {
            claims.Add(new Claim("employeeId", employee.Id.ToString()));
        }

        var permissions = user.Role.RolePermissions.Select(rp => rp.Permission.Name).ToList();
        foreach (var perm in permissions)
        {
            claims.Add(new Claim("permissions", perm));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiry = DateTime.UtcNow.AddMinutes(15);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Expiry = expiry
        };
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
            ValidateLifetime = false 
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}
