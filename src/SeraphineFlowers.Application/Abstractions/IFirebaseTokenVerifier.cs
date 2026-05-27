namespace SeraphineFlowers.Application.Abstractions;

public interface IFirebaseTokenVerifier
{
    Task<FirebaseCustomerIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
}

public sealed record FirebaseCustomerIdentity(string Uid, string PhoneNumber);
