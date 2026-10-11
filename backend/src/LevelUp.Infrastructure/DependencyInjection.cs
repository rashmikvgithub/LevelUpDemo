using LevelUp.Application.Common.Interfaces;
using LevelUp.Application.Recommendations;
using LevelUp.Infrastructure.Persistence;
using LevelUp.Infrastructure.Persistence.Repositories;
using LevelUp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LevelUp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LevelUp")
            ?? throw new InvalidOperationException("Connection string 'LevelUp' is not configured.");

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<LevelUpDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(LevelUpDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            });
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LevelUpDbContext>());
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IActivityScoreRepository, ActivityScoreRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IRecommendationHistoryRepository, RecommendationHistoryRepository>();
        services.AddHttpClient<IRecommendationProvider, OpenAiRecommendationProvider>();

        return services;
    }
}
