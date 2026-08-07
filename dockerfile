# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies (cached layer)
COPY ["WebNetTest/WebNetTest.csproj", "WebNetTest/"]
RUN dotnet restore "WebNetTest/WebNetTest.csproj"

# Copy the rest of the source code
COPY . .

# Build the project in Release configuration
RUN dotnet build "WebNetTest/WebNetTest.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "WebNetTest/WebNetTest.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Configure the application to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Redis connection string (matches the "redis" service in docker-compose.sev.yml)
ENV ConnectionStrings__Redis=redis:6379

# Expose the port the app runs on
EXPOSE 8080

# Copy the published application from the publish stage
COPY --from=publish /app/publish .

# Run the application
ENTRYPOINT ["dotnet", "WebNetTest.dll"]
