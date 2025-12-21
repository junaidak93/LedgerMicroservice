using System.Security.Cryptography;
using Ledger.API.DTOs;
using Ledger.API.Helpers;
using Ledger.API.Models;
using Ledger.API.Repositories;

namespace Ledger.API.Services;

public class AuthService : IAuthService
{
    private readonly ILoginRepository _loginRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtHelper _jwtHelper;

    public AuthService(
        ILoginRepository loginRepository,
        IRefreshTokenRepository refreshTokenRepository,
        JwtHelper jwtHelper)
    {
        _loginRepository = loginRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtHelper = jwtHelper;
    }

    public async Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        if (await _loginRepository.ExistsAsync(registerDto.Email))
        {
            throw new InvalidOperationException("Email already registered");
        }

        var login = new Login
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email,
            PasswordHash = PasswordHasher.HashPassword(registerDto.Password),
            Role = Role.User,
            Balance = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _loginRepository.CreateAsync(login);

        return await GenerateTokensAsync(login);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
    {
        var login = await _loginRepository.GetByEmailAsync(loginDto.Email);
        if (login == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!PasswordHasher.VerifyPassword(loginDto.Password, login.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // If the stored password is legacy (SHA256), rehash using PBKDF2 and update record
        if (!login.PasswordHash.StartsWith("pbkdf2_sha256$", StringComparison.Ordinal))
        {
            login.PasswordHash = PasswordHasher.HashPassword(loginDto.Password);
            await _loginRepository.UpdateAsync(login);
        }

        return await GenerateTokensAsync(login);
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        var user = token.User;
        if (user == null)
        {
            throw new InvalidOperationException("User not found for refresh token");
        }

        // Sliding expiration: extend expiry on use
        var newExpiresAt = DateTime.UtcNow.AddDays(7);
        if (token.ExpiresAt < newExpiresAt)
        {
            token.ExpiresAt = newExpiresAt;
            await _refreshTokenRepository.UpdateAsync(token);
        }

        // Generate new access token
        var accessToken = _jwtHelper.GenerateAccessToken(user);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = token.ExpiresAt
        };
    }

    private async Task<TokenResponseDto> GenerateTokensAsync(Login login)
    {
        var accessToken = _jwtHelper.GenerateAccessToken(login);
        var refreshTokenValue = GenerateRefreshToken();
        var expiresAt = _jwtHelper.GetRefreshTokenExpiration();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = login.Id,
            Token = refreshTokenValue,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await _refreshTokenRepository.CreateAsync(refreshToken);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = expiresAt
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}

