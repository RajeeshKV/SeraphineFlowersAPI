using Microsoft.Extensions.DependencyInjection;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Application.Advertisements;
using MonsoonMasala.Application.Auth;
using MonsoonMasala.Application.Common;
using MonsoonMasala.Application.Dishes;
using MonsoonMasala.Application.Menus;

namespace MonsoonMasala.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetPublicMenuQuery, IReadOnlyList<MenuDto>>, GetPublicMenuQueryHandler>();
        services.AddScoped<IQueryHandler<GetPublicMenusQuery, IReadOnlyList<MenuSummaryDto>>, GetPublicMenusQueryHandler>();
        services.AddScoped<IQueryHandler<GetPublicMenuDishesQuery, PaginatedResult<DishDto>>, GetPublicMenuDishesQueryHandler>();
        services.AddScoped<IQueryHandler<GetAllMenusQuery, IReadOnlyList<AdminMenu>>, GetAllMenusQueryHandler>();
        services.AddScoped<IQueryHandler<GetDishesByMenuQuery, PaginatedResult<DishDto>>, GetDishesByMenuQueryHandler>();
        services.AddScoped<ICommandHandler<CreateMenuCommand, MenuDto>, CreateMenuCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateMenuCommand, MenuDto>, UpdateMenuCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteMenuCommand, bool>, DeleteMenuCommandHandler>();
        services.AddScoped<ICommandHandler<CreateDishCommand, DishDto>, CreateDishCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateDishCommand, DishDto>, UpdateDishCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteDishCommand, bool>, DeleteDishCommandHandler>();
        services.AddScoped<ICommandHandler<FixDishOrdersCommand, FixDishOrdersResult>, FixDishOrdersCommandHandler>();
        services.AddScoped<ICommandHandler<CreateAdminUserCommand, AdminUserDto>, CreateAdminUserCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, AuthResponse>, RefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand, bool>, LogoutCommandHandler>();
        services.AddScoped<ICommandHandler<ResetPasswordCommand, bool>, ResetPasswordCommandHandler>();

        // Advertisement handlers
        services.AddScoped<IQueryHandler<GetAdvertisementsQuery, PaginatedResult<AdvertisementDto>>, GetAdvertisementsQueryHandler>();
        services.AddScoped<IQueryHandler<GetAdvertisementByIdQuery, AdvertisementDto?>, GetAdvertisementByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetCurrentAdvertisementQuery, AdvertisementDto?>, GetCurrentAdvertisementQueryHandler>();
        services.AddScoped<ICommandHandler<CreateAdvertisementCommand, AdvertisementDto>, CreateAdvertisementCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateAdvertisementCommand, AdvertisementDto>, UpdateAdvertisementCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteAdvertisementCommand, bool>, DeleteAdvertisementCommandHandler>();

        return services;
    }
}
