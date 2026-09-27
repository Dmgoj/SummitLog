using System.Globalization;

namespace SummitLog.GeoNamesIngest;

public static class GeoNamesLineParser
{
    private static readonly HashSet<string> MountainFeatureCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MT", "MTS", "PK"
    };

    // GeoNames allCountries.txt columns (0-indexed):
    // 0 geonameid, 1 name, 3 alternatenames, 4 latitude, 5 longitude,
    // 6 feature class, 7 feature code, 8 country code,
    // 15 elevation, 16 dem, 17 timezone, 18 modification date
    public static GeoNamesRecord? TryParseMountainLine(string line)
    {
        var fields = line.Split('\t');
        if (fields.Length < 19)
        {
            return null;
        }

        if (fields[6] != "T" || !MountainFeatureCodes.Contains(fields[7]))
        {
            return null;
        }

        if (!long.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var geoNameId))
        {
            return null;
        }

        if (!double.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
            !double.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            return null;
        }

        int? elevation = null;
        if (int.TryParse(fields[15], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ele) && IsValidElevation(ele))
        {
            elevation = ele;
        }
        else if (int.TryParse(fields[16], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dem) && IsValidElevation(dem))
        {
            elevation = dem;
        }

        var modifiedAt = DateTime.TryParse(fields[18], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            ? DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc)
            : DateTime.UtcNow;

        return new GeoNamesRecord(
            geoNameId,
            fields[1],
            fields[3],
            latitude,
            longitude,
            fields[7],
            fields[8],
            elevation,
            fields[17],
            modifiedAt);
    }

    // GeoNames' elevation/dem columns use 0 for "not set" and DEM sentinels such as -9999 or
    // -32768 for "no data at this pixel" - neither is a real elevation, and no terrestrial
    // mountain feature is anywhere near -1000m, so treat anything that low as invalid too.
    private static bool IsValidElevation(int value) => value != 0 && value > -1000;
}
