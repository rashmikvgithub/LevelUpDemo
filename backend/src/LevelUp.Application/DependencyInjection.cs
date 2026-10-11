using FluentValidation;
using LevelUp.Application.Activities;
using LevelUp.Application.Dashboard;
using LevelUp.Application.Goals;
using LevelUp.Application.Recommendations;
using Microsoft.Extensions.DependencyInjection;

namespace LevelUp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);

        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IRecommendationService, RecommendationService>();

        return services;
    }
}
