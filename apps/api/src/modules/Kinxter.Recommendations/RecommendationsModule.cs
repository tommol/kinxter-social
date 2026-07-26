using Kinxter.Recommendations.Application;
using Kinxter.Recommendations.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Kinxter.Recommendations;

public static class RecommendationsModule
{
    public static IServiceCollection AddRecommendationsModule(this IServiceCollection services) { services.AddScoped<IRecommendationsService, RecommendationsService>(); return services; }
}
