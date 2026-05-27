using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Advertisements;

public sealed record DeleteAdvertisementCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteAdvertisementCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteAdvertisementCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteAdvertisementCommand command, CancellationToken cancellationToken = default)
    {
        var advertisement = await dbContext.Advertisements
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (advertisement is null)
        {
            return false;
        }

        dbContext.Advertisements.Remove(advertisement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
