using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Infrastructure.Persistence;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class StorefrontCustomersController(
    AppDbContext dbContext,
    IOptions<StorefrontOptions> storefrontOptions,
    IFirebaseTokenVerifier firebaseTokenVerifier) : ControllerBase
{
    private readonly StorefrontOptions _storefrontOptions = storefrontOptions.Value;

    [HttpPost("customers/register")]
    public async Task<ActionResult<CustomerRegistrationResponse>> RegisterCustomer(RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (!StorefrontSupport.IsValidIndianPhone(normalizedPhone) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "A valid 10-digit Indian mobile number and name are required." });
        }

        FirebaseCustomerIdentity? identity = null;
        if (_storefrontOptions.RequireCustomerOtp)
        {
            if (string.IsNullOrWhiteSpace(request.FirebaseIdToken))
            {
                return BadRequest(new { error = "Firebase ID token is required because customer OTP verification is enabled." });
            }

            identity = await firebaseTokenVerifier.VerifyAsync(request.FirebaseIdToken, cancellationToken);
            if (StorefrontSupport.NormalizePhone(identity.PhoneNumber) != normalizedPhone)
            {
                return Unauthorized(new { error = "Firebase verified phone number does not match the requested phone number." });
            }
        }

        var existing = await dbContext.Customers.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (existing is not null)
        {
            if (identity is not null)
            {
                existing.FirebaseUid = identity.Uid;
                existing.IsOtpVerified = true;
                existing.LastLoginAt = DateTimeOffset.UtcNow;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Ok(new CustomerRegistrationResponse(true, false, StorefrontSupport.ToCustomerDto(existing, _storefrontOptions.Offer, true)));
        }

        var customer = new SeraphineFlowers.Domain.Entities.Customer
        {
            FirebaseUid = identity?.Uid,
            Phone = normalizedPhone,
            Name = request.Name.Trim(),
            IsOtpVerified = identity is not null,
            LastLoginAt = identity is not null ? DateTimeOffset.UtcNow : null,
            OfferCode = "welcome"
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new CustomerRegistrationResponse(true, true, StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true)));
    }

    [HttpPost("customers/lookup")]
    public async Task<ActionResult<CustomerLookupResponse>> LookupCustomer(CustomerLookupRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (_storefrontOptions.RequireCustomerOtp)
        {
            if (string.IsNullOrWhiteSpace(request.FirebaseIdToken))
            {
                return BadRequest(new { error = "Firebase ID token is required because customer OTP verification is enabled." });
            }

            var identity = await firebaseTokenVerifier.VerifyAsync(request.FirebaseIdToken, cancellationToken);
            if (StorefrontSupport.NormalizePhone(identity.PhoneNumber) != normalizedPhone)
            {
                return Unauthorized(new { error = "Firebase verified phone number does not match the requested phone number." });
            }
        }

        var customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        return Ok(new CustomerLookupResponse(customer is not null, customer is null ? null : StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true)));
    }

    [HttpGet("customers/{phone}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(string phone, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(phone);
        var customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        return Ok(StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true));
    }
}
