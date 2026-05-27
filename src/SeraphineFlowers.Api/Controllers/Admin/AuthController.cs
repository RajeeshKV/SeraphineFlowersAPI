using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Application.Auth;

namespace SeraphineFlowers.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public sealed class AuthController(
    SeraphineFlowers.Infrastructure.Persistence.AppDbContext dbContext,
    ICommandHandler<CreateAdminUserCommand, AdminUserDto> createUserHandler,
    ICommandHandler<LoginCommand, AuthResponse> loginHandler,
    ICommandHandler<RefreshTokenCommand, AuthResponse> refreshTokenHandler,
    ICommandHandler<LogoutCommand, bool> logoutHandler,
    ICommandHandler<ResetPasswordCommand, bool> resetPasswordHandler) : ControllerBase
{
    [HttpPost("bootstrap")]
    public async Task<ActionResult<AdminUserDto>> Bootstrap(CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.AdminUsers.AnyAsync(cancellationToken))
        {
            return Conflict(new { error = "Bootstrap admin already exists. Use the protected admin user endpoint instead." });
        }

        var user = await createUserHandler.HandleAsync(new CreateAdminUserCommand(request.Username, request.Password), cancellationToken);
        return CreatedAtAction(nameof(Bootstrap), new { id = user.Id }, user);
    }

    [Authorize(Roles = "Admin")]
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
