using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Application.Common;
using SeraphineFlowers.Domain.Entities;
using SeraphineFlowers.Infrastructure.Persistence;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/storefront")]
public sealed class StorefrontCustomersController(
    AppDbContext dbContext,
    IOptions<StorefrontOptions> storefrontOptions) : ControllerBase
{
    private readonly StorefrontOptions _storefrontOptions = storefrontOptions.Value;

    [HttpGet("customers")]
    public async Task<ActionResult<PaginatedResult<CustomerDto>>> GetCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var totalCount = await dbContext.Customers.AsNoTracking().CountAsync(cancellationToken);
        var customers = await dbContext.Customers.AsNoTracking()
            .OrderByDescending(item => item.RegisteredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(Pagination.Create(customers.Select(item => StorefrontSupport.ToCustomerDto(item, _storefrontOptions.Offer, true)).ToArray(), page, pageSize, totalCount));
    }

    [HttpPost("customers")]
    public async Task<ActionResult<CustomerDto>> UpsertCustomer(AdminCustomerUpsertRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (!StorefrontSupport.IsValidIndianPhone(normalizedPhone))
        {
            return BadRequest(new { error = "A valid 10-digit Indian mobile number is required." });
        }

        var customer = await dbContext.Customers.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (customer is null)
        {
            customer = new Customer
            {
                Phone = normalizedPhone,
                RegisteredAt = request.RegisteredAt ?? DateTimeOffset.UtcNow
            };
            dbContext.Customers.Add(customer);
        }

        customer.Name = request.Name.Trim();
        customer.OrderCount = Math.Max(0, request.OrderCount);
        customer.Notes = request.Notes?.Trim() ?? string.Empty;
        customer.LastOrderAt = request.LastOrderAt;
        customer.OfferCode = StorefrontSupport.ComputeOfferCode(customer.OrderCount, _storefrontOptions.Offer);
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true));
    }

    [HttpPost("customers/{id:guid}/orders/increment")]
    public async Task<ActionResult<CustomerDto>> IncrementOrder(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        customer.OrderCount += 1;
        customer.LastOrderAt = DateTimeOffset.UtcNow;
        customer.OfferCode = StorefrontSupport.ComputeOfferCode(customer.OrderCount, _storefrontOptions.Offer);
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true));
    }

    [HttpDelete("customers/{id:guid}")]
    public async Task<IActionResult> DeleteCustomer(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        dbContext.Customers.Remove(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
