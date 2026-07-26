namespace Kinxter.Locations.Contracts;

public sealed record PlaceState(long PlaceId, string Name, string? AdminRegion, string CountryCode, double Latitude, double Longitude)
{
    public string DisplayName => string.Join(", ", new[] { Name, AdminRegion, CountryCode }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public interface ILocationsService
{
    Task<PlaceState?> GetForEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task SetForEntityAsync(string entityType, Guid entityId, long placeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PlaceState>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default);
    Task<double> GetDistanceKmAsync(long fromPlaceId, long toPlaceId, CancellationToken cancellationToken = default);
}
