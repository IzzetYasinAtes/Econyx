namespace Econyx.Application;

using Econyx.Application.Behaviors;
using Econyx.Application.Strategies;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddTransient<RuleBasedStrategy>();
        services.AddTransient<AiAnalysisStrategy>();
        services.AddTransient<HybridStrategy>();
        services.AddTransient<IStrategy, HybridStrategy>();

        return services;
    }
}
