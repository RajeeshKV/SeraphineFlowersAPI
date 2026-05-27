using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Auth;

public sealed record LogoutCommand(Guid UserId, string? RefreshToken) : ICommand<bool>;

public sealed class LogoutCommandHandler(IAppDbContext dbContext, IAuthTokenService tokenService) : ICommandHandler<LogoutCommand, bool>
{
    public async Task<bool> HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.AdminUsers
            .Include(user => user.RefreshTokens)
            .FirstOrDefaultAsync(user => user.Id == command.UserId && user.IsActive, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        user.TokenVersion += 1;
        var now = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            foreach (var token in user.RefreshTokens.Where(token => token.RevokedAt is null && token.ExpiresAt > now))
            {
                token.RevokedAt = now;
            }
        }
        else
        {
            var refreshTokenHash = tokenService.HashRefreshToken(command.RefreshToken);
            var token = user.RefreshTokens.FirstOrDefault(token => token.TokenHash == refreshTokenHash);
            if (token is not null && token.RevokedAt is null)
            {
                token.RevokedAt = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
