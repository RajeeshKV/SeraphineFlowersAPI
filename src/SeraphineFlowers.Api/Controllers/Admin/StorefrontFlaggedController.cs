using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Application.Common;
using SeraphineFlowers.Domain.Entities;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/storefront")]
public sealed class StorefrontFlaggedController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("flagged")]
    public async Task<ActionResult<PaginatedResult<FlaggedCustomer>>> GetFlagged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var totalCount = await dbContext.FlaggedCustomers.AsNoTracking().CountAsync(cancellationToken);
        var flagged = await dbContext.FlaggedCustomers.AsNoTracking()
            .OrderByDescending(item => item.FlaggedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(Pagination.Create(flagged, page, pageSize, totalCount));
    }

    [HttpPost("flagged")]
    public async Task<ActionResult<FlaggedCustomer>> AddFlagged(AddFlaggedCustomerRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (!StorefrontSupport.IsValidIndianPhone(normalizedPhone))
        {
            return BadRequest(new { error = "A valid 10-digit Indian mobile number is required." });
        }

        var flagged = await dbContext.FlaggedCustomers.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (flagged is null)
        {
            flagged = new FlaggedCustomer
            {
                Phone = normalizedPhone,
                Reason = request.Reason?.Trim() ?? string.Empty
            };
            dbContext.FlaggedCustomers.Add(flagged);
        }
        else
        {
            flagged.Reason = request.Reason?.Trim() ?? flagged.Reason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(flagged);
    }

    [HttpDelete("flagged/{phone}")]
    public async Task<IActionResult> DeleteFlagged(string phone, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(phone);
        var flagged = await dbContext.FlaggedCustomers.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (flagged is null)
        {
            return NotFound();
        }

        dbContext.FlaggedCustomers.Remove(flagged);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
