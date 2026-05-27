using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Application.Common;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/storefront")]
public sealed class StorefrontReviewsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("reviews")]
    public async Task<ActionResult<PaginatedResult<ReviewDto>>> GetReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var totalCount = await dbContext.CustomerReviews.AsNoTracking().CountAsync(cancellationToken);
        var reviews = await dbContext.CustomerReviews.AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(Pagination.Create(reviews.Select(StorefrontSupport.ToReviewDto).ToArray(), page, pageSize, totalCount));
    }

    [HttpPost("reviews/{id:guid}/approve")]
    public async Task<IActionResult> ApproveReview(Guid id, CancellationToken cancellationToken)
    {
        return await UpdateReviewStatus(id, "approved", cancellationToken);
    }

    [HttpPost("reviews/{id:guid}/reject")]
    public async Task<IActionResult> RejectReview(Guid id, CancellationToken cancellationToken)
    {
        return await UpdateReviewStatus(id, "rejected", cancellationToken);
    }

    [HttpDelete("reviews/{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var review = await dbContext.CustomerReviews.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        dbContext.CustomerReviews.Remove(review);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult> UpdateReviewStatus(Guid id, string status, CancellationToken cancellationToken)
    {
        var review = await dbContext.CustomerReviews.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        review.Status = status;
        review.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(StorefrontSupport.ToReviewDto(review));
    }
}
