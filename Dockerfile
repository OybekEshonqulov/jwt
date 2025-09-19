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

# Scriptlarni copy qilish
COPY Scripts/ /app/Scripts/
COPY CronJobs/ /app/CronJobs/

# Cron va kerakli package larni o'rnatish
RUN apt-get update && \
    apt-get install -y cron && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Scriptlarni executable qilish
RUN chmod +x /app/Scripts/log_generator.sh && \
    chmod +x /app/CronJobs/setup_cron.sh

# Entrypoint script yaratish
COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

# Environment variable - Development default qilish
ENV ASPNETCORE_ENVIRONMENT=Development

# API konteyner ichida shu portlarda tinglaydi
EXPOSE 5257

# Entrypoint ni o'zgartirish
ENTRYPOINT ["/app/entrypoint.sh"]