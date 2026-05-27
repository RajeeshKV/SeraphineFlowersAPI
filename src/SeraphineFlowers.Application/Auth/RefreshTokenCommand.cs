using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Application.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthResponse>;

public sealed class RefreshTokenCommandHandler(IAppDbContext dbContext, IAuthTokenService tokenService) : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var incomingHash = tokenService.HashRefreshToken(command.RefreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.AdminUser)
            .FirstOrDefaultAsync(token => token.TokenHash == incomingHash, cancellationToken);

        if (storedToken?.AdminUser is null || !storedToken.IsActive || !storedToken.AdminUser.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var tokens = tokenService.CreateTokens(storedToken.AdminUser);
        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        storedToken.ReplacedByTokenHash = tokenService.HashRefreshToken(tokens.RefreshToken);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            AdminUserId = storedToken.AdminUserId,
            TokenHash = storedToken.ReplacedByTokenHash,
            ExpiresAt = tokens.RefreshTokenExpiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt, tokens.RefreshTokenExpiresAt);
    }
}
