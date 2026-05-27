using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;

namespace MonsoonMasala.Application.Auth;

public sealed record ResetPasswordCommand(Guid UserId, string NewPassword) : ICommand<bool>;

public sealed class ResetPasswordCommandHandler(IAppDbContext dbContext, IPasswordHasher passwordHasher) : ICommandHandler<ResetPasswordCommand, bool>
{
    public async Task<bool> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.NewPassword) || command.NewPassword.Length < 8)
        {
            throw new ArgumentException("New password must be at least 8 characters.");
        }

        var user = await dbContext.AdminUsers
            .Include(user => user.RefreshTokens)
            .FirstOrDefaultAsync(user => user.Id == command.UserId && user.IsActive, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        user.PasswordHash = passwordHasher.Hash(command.NewPassword);
        user.TokenVersion += 1;

        var now = DateTimeOffset.UtcNow;
        foreach (var token in user.RefreshTokens.Where(token => token.RevokedAt is null && token.ExpiresAt > now))
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
