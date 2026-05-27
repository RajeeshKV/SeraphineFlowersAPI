using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Application.Auth;

public sealed record LoginCommand(string Username, string Password) : ICommand<AuthResponse>;

public sealed class LoginCommandHandler(IAppDbContext dbContext, IPasswordHasher passwordHasher, IAuthTokenService tokenService) : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var username = command.Username.Trim().ToLowerInvariant();
        var user = await dbContext.AdminUsers.FirstOrDefaultAsync(user => user.Username == username && user.IsActive, cancellationToken);
        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var tokens = tokenService.CreateTokens(user);
        user.LastLoginAt = DateTimeOffset.UtcNow;
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            AdminUserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(tokens.RefreshToken),
            ExpiresAt = tokens.RefreshTokenExpiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt, tokens.RefreshTokenExpiresAt);
    }
}
