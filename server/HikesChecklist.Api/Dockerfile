FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY server/HikesChecklist.Api/HikesChecklist.Api.csproj server/HikesChecklist.Api/
RUN dotnet restore server/HikesChecklist.Api/HikesChecklist.Api.csproj

COPY server/HikesChecklist.Api/ server/HikesChecklist.Api/
RUN dotnet publish server/HikesChecklist.Api/HikesChecklist.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HikesChecklist.Api.dll"]
