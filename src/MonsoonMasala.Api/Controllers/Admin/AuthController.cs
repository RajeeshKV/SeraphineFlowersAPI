using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Application.Auth;

namespace MonsoonMasala.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public sealed class AuthController(
    ICommandHandler<CreateAdminUserCommand, AdminUserDto> createUserHandler,
    ICommandHandler<LoginCommand, AuthResponse> loginHandler,
    ICommandHandler<RefreshTokenCommand, AuthResponse> refreshTokenHandler,
    ICommandHandler<LogoutCommand, bool> logoutHandler,
    ICommandHandler<ResetPasswordCommand, bool> resetPasswordHandler) : ControllerBase
{
    [HttpPost("users")]
    public async Task<ActionResult<AdminUserDto>> CreateUser(CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var user = await createUserHandler.HandleAsync(new CreateAdminUserCommand(request.Username, request.Password), cancellationToken);
        return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await loginHandler.HandleAsync(new LoginCommand(request.Username, request.Password), cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await refreshTokenHandler.HandleAsync(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        await logoutHandler.HandleAsync(new LogoutCommand(userId, request.RefreshToken), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        await resetPasswordHandler.HandleAsync(new ResetPasswordCommand(userId, request.NewPassword), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateAdminUserRequest(string Username, string Password);
public sealed record LoginRequest(string Username, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string? RefreshToken);
public sealed record ResetPasswordRequest(string NewPassword);
