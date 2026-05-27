using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Infrastructure.Firebase;

public sealed class FirebaseTokenVerifier(IOptions<StorefrontOptions> storefrontOptions) : IFirebaseTokenVerifier
{
    private readonly StorefrontOptions _storefrontOptions = storefrontOptions.Value;
    private readonly object _sync = new();
    private FirebaseApp? _app;

    public async Task<FirebaseCustomerIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (!_storefrontOptions.Firebase.Enabled)
        {
            throw new InvalidOperationException("Firebase OTP verification is not enabled on the backend.");
        }

        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException("Firebase ID token is required.");
        }

        var app = GetOrCreateApp();
        var auth = FirebaseAuth.GetAuth(app);
        var decoded = await auth.VerifyIdTokenAsync(idToken);

        if (!decoded.Claims.TryGetValue("phone_number", out var phoneValue) || phoneValue?.ToString() is not string phoneNumber || string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new UnauthorizedAccessException("Verified Firebase token does not contain a phone number.");
        }

        return new FirebaseCustomerIdentity(decoded.Uid, phoneNumber);
    }

    private FirebaseApp GetOrCreateApp()
    {
        if (_app is not null)
        {
            return _app;
        }

        lock (_sync)
        {
            if (_app is not null)
            {
                return _app;
            }

            var options = new AppOptions
            {
                Credential = ResolveCredential()
            };

            if (!string.IsNullOrWhiteSpace(_storefrontOptions.Firebase.ProjectId))
            {
                options.ProjectId = _storefrontOptions.Firebase.ProjectId;
            }

            _app = FirebaseApp.Create(options, "seraphine-flowers-backend");
            return _app;
        }
    }

    private GoogleCredential ResolveCredential()
    {
        if (!string.IsNullOrWhiteSpace(_storefrontOptions.Firebase.ServiceAccountJsonBase64))
        {
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(_storefrontOptions.Firebase.ServiceAccountJsonBase64));
            return GoogleCredential.FromJson(json);
        }

        if (!string.IsNullOrWhiteSpace(_storefrontOptions.Firebase.ServiceAccountJsonPath))
        {
            return GoogleCredential.FromFile(_storefrontOptions.Firebase.ServiceAccountJsonPath);
        }

        return GoogleCredential.GetApplicationDefault();
    }
}
