# Use .NET 9 SDK for build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["ItemRepositoryWorkerService/ItemRepositoryWorkerService.csproj", "ItemRepositoryWorkerService/"]
RUN dotnet restore "ItemRepositoryWorkerService/ItemRepositoryWorkerService.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/ItemRepositoryWorkerService"
RUN dotnet publish "ItemRepositoryWorkerService.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ItemRepositoryWorkerService.dll"]
