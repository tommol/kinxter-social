FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY apps/auth/src/Kinxter.Auth/Kinxter.Auth.csproj apps/auth/src/Kinxter.Auth/
COPY apps/api/src/shared/Kinxter.IntegrationEvents/Kinxter.IntegrationEvents.csproj apps/api/src/shared/Kinxter.IntegrationEvents/
COPY apps/api/src/shared/Kinxter.Shared.Abstractions/Kinxter.Shared.Abstractions.csproj apps/api/src/shared/Kinxter.Shared.Abstractions/
COPY apps/api/src/shared/Kinxter.Shared.Infrastructure/Kinxter.Shared.Infrastructure.csproj apps/api/src/shared/Kinxter.Shared.Infrastructure/
RUN dotnet restore apps/auth/src/Kinxter.Auth/Kinxter.Auth.csproj

COPY . .
RUN dotnet publish apps/auth/src/Kinxter.Auth/Kinxter.Auth.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Kinxter.Auth.dll"]
