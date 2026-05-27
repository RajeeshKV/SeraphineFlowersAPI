using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Auth;

/// <summary>
/// Logs in a customer using phone + Firebase OTP token.
/// </summary>
public sealed record LoginCustomerWithOtpCommand(
    string Phone,
    string FirebaseIdToken
) : ICommand<CustomerAuthResponse>;

public sealed class LoginCustomerWithOtpCommandHandler(
    IAppDbContext dbContext,
    IAuthTokenService tokenService,
    IFirebaseTokenVerifier firebaseTokenVerifier) : ICommandHandler<LoginCustomerWithOtpCommand, CustomerAuthResponse>
{
    public async Task<CustomerAuthResponse> HandleAsync(LoginCustomerWithOtpCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhone(command.Phone);
        if (!IsValidIndianPhone(normalizedPhone))
        {
            throw new ArgumentException("A valid 10-digit Indian mobile number is required.");
        }

        if (string.IsNullOrWhiteSpace(command.FirebaseIdToken))
        {
            throw new ArgumentException("Firebase ID token is required for OTP verification.");
        }

        var identity = await firebaseTokenVerifier.VerifyAsync(command.FirebaseIdToken, cancellationToken);
        var tokenPhone = NormalizePhone(identity.PhoneNumber);
        if (tokenPhone != normalizedPhone)
        {
            throw new UnauthorizedAccessException("Firebase verified phone number does not match the requested phone number.");
        }

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(c => c.Phone == normalizedPhone, cancellationToken);

        if (customer is null)
        {
            throw new UnauthorizedAccessException("Customer with this phone number does not exist. Please register first.");
        }

        customer.FirebaseUid = identity.Uid;
        customer.IsOtpVerified = true;
        customer.LastLoginAt = DateTimeOffset.UtcNow;

        var tokens = tokenService.CreateCustomerTokens(customer);

        dbContext.CustomerRefreshTokens.Add(new SeraphineFlowers.Domain.Entities.CustomerRefreshToken
        {
            CustomerId = customer.Id,
            TokenHash = tokenService.HashRefreshToken(tokens.RefreshToken),
            ExpiresAt = tokens.RefreshTokenExpiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerAuthResponse(
            customer.Id,
            customer.Phone,
            customer.Name,
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt,
            true);
    }

    private static string NormalizePhone(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return string.Empty;
        return new string(rawPhone.Where(char.IsDigit).ToArray());
    }

    private static bool IsValidIndianPhone(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone) || rawPhone.Any(ch => !char.IsDigit(ch)))
            return false;
        if (rawPhone.Length != 10 || rawPhone[0] is < '6' or > '9')
            return false;
        if (rawPhone.All(ch => ch == rawPhone[0]))
            return false;
        return rawPhone is not "0123456789" and not "1234567890" and not "9876543210" and not "0987654321";
    }
}
