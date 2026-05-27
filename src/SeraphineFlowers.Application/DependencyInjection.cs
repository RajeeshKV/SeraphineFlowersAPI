using Microsoft.Extensions.DependencyInjection;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Application.Advertisements;
using SeraphineFlowers.Application.Auth;
using SeraphineFlowers.Application.Common;

namespace SeraphineFlowers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateAdminUserCommand, AdminUserDto>, CreateAdminUserCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, AuthResponse>, RefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand, bool>, LogoutCommandHandler>();
        services.AddScoped<ICommandHandler<ResetPasswordCommand, bool>, ResetPasswordCommandHandler>();

        // Customer auth handlers
        services.AddScoped<ICommandHandler<VerifyCustomerOtpCommand, CustomerAuthResponse>, VerifyCustomerOtpCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCustomerWithOtpCommand, CustomerAuthResponse>, LoginCustomerWithOtpCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshCustomerTokenCommand, CustomerAuthResponse>, RefreshCustomerTokenCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCustomerCommand, bool>, LogoutCustomerCommandHandler>();

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

