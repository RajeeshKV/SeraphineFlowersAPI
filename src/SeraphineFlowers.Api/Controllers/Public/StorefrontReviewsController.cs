using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Application.Common;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class StorefrontReviewsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("reviews")]
    public async Task<ActionResult<PaginatedResult<ReviewDto>>> GetReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.CustomerReviews.AsNoTracking().Where(item => item.Status == "approved");
        var totalCount = await query.CountAsync(cancellationToken);
        var reviews = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(Pagination.Create(reviews.Select(StorefrontSupport.ToReviewDto).ToArray(), page, pageSize, totalCount));
    }

    [HttpPost("reviews")]
    public async Task<ActionResult<ReviewDto>> SubmitReview(SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (!StorefrontSupport.IsValidIndianPhone(normalizedPhone) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Text) ||
            request.Rating < 1 ||
            request.Rating > 5)
        {
            return BadRequest(new { error = "Phone, name, rating, and review text are required." });
        }

        var existing = await dbContext.CustomerReviews.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (existing is not null)
        {
            return Conflict(new { error = "You have already submitted a review." });
        }

        var review = new SeraphineFlowers.Domain.Entities.CustomerReview
        {
            Phone = normalizedPhone,
            Name = request.Name.Trim(),
            Rating = request.Rating,
            Text = request.Text.Trim(),
            Status = "approved"
        };

        dbContext.CustomerReviews.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(StorefrontSupport.ToReviewDto(review));
    }
}
