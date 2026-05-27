using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Application.Abstractions;

public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, DateTimeOffset RefreshTokenExpiresAt);

public interface IAuthTokenService
{
    AuthTokens CreateTokens(AdminUser user);
    string HashRefreshToken(string refreshToken);
}
