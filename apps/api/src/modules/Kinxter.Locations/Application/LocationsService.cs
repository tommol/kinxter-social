using Kinxter.Locations.Contracts;
using Kinxter.Locations.Infrastructure;
using Kinxter.Locations.Model;
using Kinxter.Shared.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Locations.Application;

internal sealed class LocationsService(LocationsDbContext dbContext, IClock clock) : ILocationsService
{
    public async Task<PlaceState?> GetForEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default) =>
        await (from location in dbContext.EntityLocations.AsNoTracking()
               join place in dbContext.Places.AsNoTracking() on location.PlaceId equals place.GeoNameId
               where location.EntityType == entityType && location.EntityId == entityId
               select new PlaceState(place.GeoNameId, place.Name, place.AdminRegion, place.CountryCode, place.Latitude, place.Longitude))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SetForEntityAsync(string entityType, Guid entityId, long placeId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Places.AnyAsync(place => place.GeoNameId == placeId, cancellationToken)) throw new ArgumentException("Place does not exist.", nameof(placeId));
        var location = await dbContext.EntityLocations.SingleOrDefaultAsync(current => current.EntityType == entityType && current.EntityId == entityId, cancellationToken);
        if (location is null) dbContext.EntityLocations.Add(new EntityLocation(entityType, entityId, placeId, clock.UtcNow));
        else location.ChangePlace(placeId, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlaceState>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim();
        if (normalized.Length < 2) return [];
        return await dbContext.Places.AsNoTracking()
            .Where(place => EF.Functions.ILike(place.Name, $"{normalized}%") || (place.AdminRegion != null && EF.Functions.ILike(place.AdminRegion, $"{normalized}%")))
            .OrderBy(place => place.Name).Take(Math.Clamp(limit, 1, 20))
            .Select(place => new PlaceState(place.GeoNameId, place.Name, place.AdminRegion, place.CountryCode, place.Latitude, place.Longitude))
            .ToArrayAsync(cancellationToken);
    }

    public Task<double> GetDistanceKmAsync(long fromPlaceId, long toPlaceId, CancellationToken cancellationToken = default) =>
        dbContext.Database.SqlQuery<double>($"""
            SELECT ST_Distance(origin.centroid, destination.centroid) / 1000.0 AS "Value"
            FROM locations.places AS origin
            CROSS JOIN locations.places AS destination
            WHERE origin."GeoNameId" = {fromPlaceId}
              AND destination."GeoNameId" = {toPlaceId}
            """).SingleAsync(cancellationToken);
}
