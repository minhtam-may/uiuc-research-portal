using Microsoft.Extensions.DependencyInjection;
using UiucResearchPortal.Application.Services;

namespace UiucResearchPortal.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ResearchChatService>();
        return services;
    }
}
