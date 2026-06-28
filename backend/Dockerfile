FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files in dependency order
COPY src/ECommerce.Domain/ECommerce.Domain.csproj src/ECommerce.Domain/
COPY src/ECommerce.Application/ECommerce.Application.csproj src/ECommerce.Application/
COPY src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj src/ECommerce.Infrastructure/
COPY src/ECommerce.API/ECommerce.API.csproj src/ECommerce.API/

# Restore packages
RUN dotnet restore src/ECommerce.API/ECommerce.API.csproj

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/ECommerce.API/ECommerce.API.csproj -c Release -o /app/publish --no-restore

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]