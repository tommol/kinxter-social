using System.Globalization;
using Kinxter.Locations.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Locations.Infrastructure;

public static class LocationsSeed
{
    public static async Task SeedAsync(LocationsDbContext dbContext, string? geoNamesFile, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Places.AnyAsync(cancellationToken)) return;

        if (!string.IsNullOrWhiteSpace(geoNamesFile) && File.Exists(geoNamesFile))
        {
            await ImportGeoNamesAsync(dbContext, geoNamesFile, cancellationToken);
            return;
        }

        dbContext.Places.AddRange(
            new Place(756135, "Warsaw", "Masovian Voivodeship", "PL", 52.22977, 21.01178),
            new Place(3094802, "Kraków", "Lesser Poland", "PL", 50.06143, 19.93658),
            new Place(3081368, "Wrocław", "Lower Silesia", "PL", 51.10789, 17.03854),
            new Place(3099434, "Gdańsk", "Pomerania", "PL", 54.35205, 18.64637),
            new Place(3088171, "Poznań", "Greater Poland", "PL", 52.40692, 16.92993),
            new Place(2950159, "Berlin", "Berlin", "DE", 52.52437, 13.41053),
            new Place(3067696, "Prague", "Prague", "CZ", 50.08804, 14.42076));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task ImportGeoNamesAsync(LocationsDbContext dbContext, string file, CancellationToken token)
    {
        var batch = new List<Place>(1000);
        foreach (var line in File.ReadLines(file))
        {
            token.ThrowIfCancellationRequested(); var columns = line.Split('\t');
            if (columns.Length < 15 || !long.TryParse(columns[0], out var id) || !double.TryParse(columns[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) || !double.TryParse(columns[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) continue;
            batch.Add(new Place(id, columns[1], columns[10], columns[8], lat, lon));
            if (batch.Count < 1000) continue;
            dbContext.Places.AddRange(batch); await dbContext.SaveChangesAsync(token); dbContext.ChangeTracker.Clear(); batch.Clear();
        }
        if (batch.Count > 0) { dbContext.Places.AddRange(batch); await dbContext.SaveChangesAsync(token); }
    }
}
