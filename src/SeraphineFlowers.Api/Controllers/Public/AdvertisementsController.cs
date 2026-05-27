using Microsoft.AspNetCore.Mvc;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Application.Advertisements;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public/advertisements")]
public sealed class AdvertisementsController(
    IQueryHandler<GetCurrentAdvertisementQuery, AdvertisementDto?> getCurrentAdvertisementHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdvertisementDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var advertisement = await getCurrentAdvertisementHandler.HandleAsync(
            new GetCurrentAdvertisementQuery(),
            cancellationToken);

        if (advertisement is null)
        {
            return NotFound();
        }

        return Ok(advertisement);
    }
}
