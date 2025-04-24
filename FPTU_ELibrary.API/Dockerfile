# Base image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
USER app
EXPOSE 80

# Image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY FPTU_ELibrary.API/FPTU_ELibrary.API.csproj FPTU_ELibrary.API/
COPY FPTU_ELibrary.Application/FPTU_ELibrary.Application.csproj FPTU_ELibrary.Application/
COPY FPTU_ELibrary.Domain/FPTU_ELibrary.Domain.csproj FPTU_ELibrary.Domain/
COPY FPTU_ELibrary.Infrastructure/FPTU_ELibrary.Infrastructure.csproj FPTU_ELibrary.Infrastructure/

RUN #mkdir -p /app/certificates
#COPY FPTU_ELibrary.API/aspnetapp.pfx /app/certificates

# Restore dependencies
RUN dotnet restore FPTU_ELibrary.API/FPTU_ELibrary.API.csproj

# Copy the remaining source code and build the application
COPY . .
WORKDIR /src/FPTU_ELibrary.API
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image for running the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FPTU_ELibrary.API.dll"]
