FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

# Copy all necessary files
COPY . .

# Explicitly copy the NLog.config file into the container


# Restore dependencies
RUN dotnet restore

# Publish the application
RUN dotnet publish -c Release -o /app/published-app

# Stage for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Copy published files from the build stage
COPY --from=build /app/published-app /app

# Set entry point for the application
ENTRYPOINT ["dotnet", "BidWorker.dll"]
