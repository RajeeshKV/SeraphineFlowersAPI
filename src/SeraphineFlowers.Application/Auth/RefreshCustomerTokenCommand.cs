using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Auth;

public sealed record RefreshCustomerTokenCommand(string RefreshToken) : ICommand<CustomerAuthResponse>;

public sealed class RefreshCustomerTokenCommandHandler(
    IAppDbContext dbContext,
    IAuthTokenService tokenService) : ICommandHandler<RefreshCustomerTokenCommand, CustomerAuthResponse>
{
    public async Task<CustomerAuthResponse> HandleAsync(RefreshCustomerTokenCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = tokenService.HashRefreshToken(command.RefreshToken);
        var storedToken = await dbContext.CustomerRefreshTokens
            .Include(rt => rt.Customer)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.IsActive, cancellationToken);

        if (storedToken?.Customer is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var customer = storedToken.Customer;
        var newTokens = tokenService.CreateCustomerTokens(customer);

        // Revoke old token and create new one
        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        storedToken.ReplacedByTokenHash = tokenService.HashRefreshToken(newTokens.RefreshToken);

        dbContext.CustomerRefreshTokens.Add(new SeraphineFlowers.Domain.Entities.CustomerRefreshToken
        {
            CustomerId = customer.Id,
            TokenHash = tokenService.HashRefreshToken(newTokens.RefreshToken),
            ExpiresAt = newTokens.RefreshTokenExpiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerAuthResponse(
            customer.Id,
            customer.Phone,
            customer.Name,
            newTokens.AccessToken,
            newTokens.RefreshToken,
            newTokens.AccessTokenExpiresAt,
            newTokens.RefreshTokenExpiresAt,
            customer.IsOtpVerified);
    }
}
