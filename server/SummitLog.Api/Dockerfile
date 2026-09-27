FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY server/SummitLog.Api/SummitLog.Api.csproj server/SummitLog.Api/
RUN dotnet restore server/SummitLog.Api/SummitLog.Api.csproj

COPY server/SummitLog.Api/ server/SummitLog.Api/
RUN dotnet publish server/SummitLog.Api/SummitLog.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SummitLog.Api.dll"]
