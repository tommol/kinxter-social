namespace Kinxter.Locations.Model;

public sealed class Place
{
    private Place() { Name = CountryCode = null!; }
    public Place(long geoNameId, string name, string? adminRegion, string countryCode, double latitude, double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(latitude));
        GeoNameId = geoNameId;
        Name = name.Trim();
        AdminRegion = string.IsNullOrWhiteSpace(adminRegion) ? null : adminRegion.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        Latitude = latitude;
        Longitude = longitude;
    }
    public long GeoNameId { get; private set; }
    public string Name { get; private set; }
    public string? AdminRegion { get; private set; }
    public string CountryCode { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string DisplayName => string.Join(", ", new[] { Name, AdminRegion, CountryCode }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed class EntityLocation
{
    private EntityLocation() { EntityType = null!; }
    public EntityLocation(string entityType, Guid entityId, long placeId, DateTimeOffset updatedAt)
    {
        EntityType = entityType; EntityId = entityId; PlaceId = placeId; UpdatedAt = updatedAt;
    }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public long PlaceId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public void ChangePlace(long placeId, DateTimeOffset updatedAt) { PlaceId = placeId; UpdatedAt = updatedAt; }
}
