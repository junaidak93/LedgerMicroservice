using Microsoft.Extensions.Configuration;
using Moq;
using Ledger.API.DTOs;
using Ledger.API.Helpers;
using Ledger.API.Models;
using Ledger.API.Repositories;
using Ledger.API.Services;
using Ledger.Tests.Helpers;
using Xunit;

namespace Ledger.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<ILoginRepository> _loginRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly JwtHelper _jwtHelper;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _loginRepositoryMock = new Mock<ILoginRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["Jwt:SecretKey"]).Returns("YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("LedgerAPI");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("LedgerAPI");
        _configurationMock.Setup(c => c["Jwt:AccessTokenExpirationMinutes"]).Returns("15");
        _configurationMock.Setup(c => c["Jwt:RefreshTokenExpirationDays"]).Returns("7");

        _jwtHelper = new JwtHelper(_configurationMock.Object);
        _authService = new AuthService(
            _loginRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _jwtHelper);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsTokens()
    {
        // Arrange
        var registerDto = new RegisterDto { Email = "test@example.com", Password = "password123" };
        _loginRepositoryMock.Setup(r => r.ExistsAsync(registerDto.Email)).ReturnsAsync(false);
        _loginRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Login>())).ReturnsAsync((Login login) => login);
        _refreshTokenRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken token) => token);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        _loginRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Login>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ThrowsException()
    {
        // Arrange
        var registerDto = new RegisterDto { Email = "test@example.com", Password = "password123" };
        _loginRepositoryMock.Setup(r => r.ExistsAsync(registerDto.Email)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(registerDto));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
        var user = TestHelpers.CreateTestUser(loginDto.Email);
        user.PasswordHash = PasswordHasher.HashPassword(loginDto.Password);

        _loginRepositoryMock.Setup(r => r.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);
        _refreshTokenRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken token) => token);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ThrowsException()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "wrongpassword" };
        _loginRepositoryMock.Setup(r => r.GetByEmailAsync(loginDto.Email)).ReturnsAsync((Login?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginDto));
    }
}

