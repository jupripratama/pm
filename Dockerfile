# Base image untuk runtime ASP.NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:$PORT

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj dan restore dependencies
COPY ["Pm.csproj", "."]
RUN dotnet restore "Pm.csproj"

# Copy semua file ke dalam container
COPY . .

# Build project
RUN dotnet build "Pm.csproj" -c Release -o /app/build

# Publish hasil build
FROM build AS publish
RUN dotnet publish "Pm.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pm.dll"]
