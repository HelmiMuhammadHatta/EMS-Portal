using EMS.Application.Auth;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests;

public class AuthServiceTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        
        _mockConfig.Setup(c => c["Jwt:Key"]).Returns("SuperSecretKeyThatIsAtLeast32BytesLongForTesting!");
        _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _mockConfig.Setup(c => c["Jwt:DurationInMinutes"]).Returns("15");

        _authService = new AuthService(_mockContext.Object, _mockConfig.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            IsActive = true,
            RoleId = role.Id,
            Role = role
        };

        _mockContext.Setup(c => c.Users).ReturnsDbSet(new List<User> { user });

        var request = new LoginRequest { Email = "test@test.com", Password = "Password123!" };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            IsActive = true
        };

        _mockContext.Setup(c => c.Users).ReturnsDbSet(new List<User> { user });

        var request = new LoginRequest { Email = "test@test.com", Password = "WrongPassword!" };

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Invalid email or password.");
    }
}
