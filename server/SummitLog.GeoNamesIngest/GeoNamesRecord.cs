namespace SummitLog.GeoNamesIngest;

public record GeoNamesRecord(
    long GeoNameId,
    string Name,
    string AlternateNames,
    double Latitude,
    double Longitude,
    string FeatureCode,
    string CountryCode,
    int? ElevationMeters,
    string Timezone,
    DateTime ModifiedAt);
