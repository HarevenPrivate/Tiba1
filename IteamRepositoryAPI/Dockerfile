# Use .NET 9 SDK for build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["IteamRepositoryAPI/IteamRepositoryAPI.csproj", "IteamRepositoryAPI/"]
RUN dotnet restore "IteamRepositoryAPI/IteamRepositoryAPI.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/IteamRepositoryAPI"
RUN dotnet publish "IteamRepositoryAPI.csproj" -c Release -o /app/publish

# Runtime image for ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IteamRepositoryAPI.dll"]
