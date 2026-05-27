using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Auth;

public sealed record LogoutCustomerCommand(Guid CustomerId, string? RefreshToken) : ICommand<bool>;

public sealed class LogoutCustomerCommandHandler(
    IAppDbContext dbContext,
    IAuthTokenService tokenService) : ICommandHandler<LogoutCustomerCommand, bool>
{
    public async Task<bool> HandleAsync(LogoutCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return true;
        }

        var tokenHash = tokenService.HashRefreshToken(command.RefreshToken);
        var token = await dbContext.CustomerRefreshTokens
            .FirstOrDefaultAsync(rt => rt.CustomerId == command.CustomerId && rt.TokenHash == tokenHash, cancellationToken);

        if (token is not null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
