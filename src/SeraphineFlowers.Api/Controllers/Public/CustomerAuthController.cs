using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Application.Auth;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public/customers/auth")]
public sealed class CustomerAuthController(
    ICommandHandler<VerifyCustomerOtpCommand, CustomerAuthResponse> verifyOtpHandler,
    ICommandHandler<LoginCustomerWithOtpCommand, CustomerAuthResponse> loginOtpHandler,
    ICommandHandler<RefreshCustomerTokenCommand, CustomerAuthResponse> refreshTokenHandler,
    ICommandHandler<LogoutCustomerCommand, bool> logoutHandler) : ControllerBase
{
    /// <summary>
    /// Register or verify a customer using Firebase OTP token.
    /// Works for both new customer registration and existing customer OTP verification.
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<ActionResult<CustomerAuthResponse>> VerifyOtp(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.FirebaseIdToken))
        {
            return BadRequest(new { error = "Phone and Firebase ID token are required." });
        }

        try
        {
            var response = await verifyOtpHandler.HandleAsync(
                new VerifyCustomerOtpCommand(request.Phone, request.Name, request.FirebaseIdToken),
                cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Login an existing customer using phone + Firebase OTP token.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<CustomerAuthResponse>> Login(LoginOtpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.FirebaseIdToken))
        {
            return BadRequest(new { error = "Phone and Firebase ID token are required." });
        }

        try
        {
            var response = await loginOtpHandler.HandleAsync(
                new LoginCustomerWithOtpCommand(request.Phone, request.FirebaseIdToken),
                cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Refresh customer access token using refresh token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<CustomerAuthResponse>> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "Refresh token is required." });
        }

        try
        {
            var response = await refreshTokenHandler.HandleAsync(
                new RefreshCustomerTokenCommand(request.RefreshToken),
                cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Logout customer by revoking their refresh token.
    /// </summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutTokenRequest request, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var customerId))
        {
            return Unauthorized();
        }

        await logoutHandler.HandleAsync(new LogoutCustomerCommand(customerId, request.RefreshToken), cancellationToken);
        return NoContent();
    }
}

public sealed record VerifyOtpRequest(string Phone, string Name, string FirebaseIdToken);
public sealed record LoginOtpRequest(string Phone, string FirebaseIdToken);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutTokenRequest(string? RefreshToken);
