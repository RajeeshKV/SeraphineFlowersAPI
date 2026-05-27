namespace MonsoonMasala.Application.Auth;

public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, DateTimeOffset RefreshTokenExpiresAt);
public sealed record AdminUserDto(Guid Id, string Username, bool IsActive, DateTimeOffset CreatedAt);
