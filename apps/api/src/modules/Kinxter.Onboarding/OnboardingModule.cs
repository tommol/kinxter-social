using Kinxter.Onboarding.Application;
using Kinxter.Onboarding.Contracts;
using Kinxter.Onboarding.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Onboarding;

public static class OnboardingModule
{
    public static IServiceCollection AddOnboardingModule(this IServiceCollection services, IConfiguration configuration)
    { var connection = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured."); services.AddDbContext<OnboardingDbContext>(options => options.UseNpgsql(connection, postgres => { postgres.MigrationsAssembly(typeof(OnboardingModule).Assembly.GetName().Name); postgres.MigrationsHistoryTable("__ef_migrations_history", "onboarding"); })); services.AddScoped<IOnboardingService, OnboardingService>(); return services; }
}
