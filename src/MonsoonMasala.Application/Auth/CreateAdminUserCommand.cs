using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Application.Auth;

public sealed record CreateAdminUserCommand(string Username, string Password) : ICommand<AdminUserDto>;

public sealed class CreateAdminUserCommandHandler(IAppDbContext dbContext, IPasswordHasher passwordHasher) : ICommandHandler<CreateAdminUserCommand, AdminUserDto>
{
    public async Task<AdminUserDto> HandleAsync(CreateAdminUserCommand command, CancellationToken cancellationToken = default)
    {
        var username = command.Username.Trim().ToLowerInvariant();
        if (await dbContext.AdminUsers.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Admin user already exists.");
        }

        var user = new AdminUser
        {
            Username = username,
            PasswordHash = passwordHasher.Hash(command.Password)
        };

        dbContext.AdminUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdminUserDto(user.Id, user.Username, user.IsActive, user.CreatedAt);
    }
}
