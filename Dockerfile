# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/AuditSystem.API/AuditSystem.API.csproj", "src/AuditSystem.API/"]
COPY ["src/AuditSystem.Domain/AuditSystem.Domain.csproj", "src/AuditSystem.Domain/"]
COPY ["src/AuditSystem.Infrastructure/AuditSystem.Infrastructure.csproj", "src/AuditSystem.Infrastructure/"]
COPY ["src/AuditSystem.Services/AuditSystem.Services.csproj", "src/AuditSystem.Services/"]
COPY ["src/AuditSystem.Common/AuditSystem.Common.csproj", "src/AuditSystem.Common/"]

RUN dotnet restore "src/AuditSystem.API/AuditSystem.API.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/src/AuditSystem.API"
RUN dotnet build "AuditSystem.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "AuditSystem.API.csproj" -c Release -o /app/publish --no-restore

# Use the official .NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "AuditSystem.API.dll"] 