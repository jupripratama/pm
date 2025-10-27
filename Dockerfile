# Gunakan .NET 8 SDK untuk build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj dan restore dependencies
COPY *.sln .
COPY ["Pm/Pm.csproj", "Pm/"]
RUN dotnet restore "Pm/Pm.csproj"

# Copy seluruh source dan build
COPY . .
WORKDIR "/app/Pm"
RUN dotnet publish "Pm.csproj" -c Release -o /out

# Gunakan runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "Pm.dll"]
