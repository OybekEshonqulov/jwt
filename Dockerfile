# Build bosqichi
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Run bosqichi
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Environment variable - Development default qilish
ENV ASPNETCORE_ENVIRONMENT=Development

# API konteyner ichida shu portlarda tinglaydi
EXPOSE 5257

ENTRYPOINT ["dotnet", "jwtDocker.dll"]