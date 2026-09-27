# SummitLog

Search mountains and peaks worldwide, log in, and keep a checklist of the ones you've visited or want to
visit. Filter and sort search results by country, elevation, and name; view any peak on a map; and set a
profile name/picture.

## Stack

- **Backend:** ASP.NET Core Web API (.NET 10), EF Core + PostgreSQL, ASP.NET Core Identity + JWT auth
- **Frontend:** React + TypeScript (Vite)
- **Peak data:** seeded from [GeoNames](https://www.geonames.org/) (see `CREDITS.md`)

## Running locally

### 1. One-time setup

Requires a Postgres database (e.g. a free [Neon](https://neon.tech) project) and an S3-compatible bucket
for avatar uploads (e.g. [Cloudflare R2](https://www.cloudflare.com/developer-platform/products/r2/)).

```
cd server/SummitLog.Api
dotnet user-secrets set "Jwt:Key" "<a long random secret>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your Postgres connection string>"
dotnet user-secrets set "Storage:ServiceUrl" "<your S3-compatible endpoint>"
dotnet user-secrets set "Storage:AccessKey" "<access key>"
dotnet user-secrets set "Storage:SecretKey" "<secret key>"
dotnet user-secrets set "Storage:BucketName" "<bucket name>"
```

### 2. Seed the peaks database

Downloads GeoNames' `allCountries.zip` (~450MB) and loads mountain/peak entries into the database. Takes a few minutes; safe to re-run.

```
cd server/SummitLog.GeoNamesIngest
dotnet run -- --data-dir ./data --connection-string "<your Postgres connection string>"
```

### 3. Run the API (applies EF Core migrations automatically)

```
cd server/SummitLog.Api
dotnet run --launch-profile https
```

API runs at `https://localhost:7255`.

### 4. Run the client

```
cd client
npm install
npm run dev
```

Client runs at `http://localhost:5173`.

## Notes

- JWTs are valid for 1 day (see `Jwt:ExpiryMinutes` in `appsettings.json`) — there's no refresh-token flow yet, just re-login after expiry.
- Peak detail pages show a Leaflet/OpenStreetMap map centered on the peak's coordinates.
