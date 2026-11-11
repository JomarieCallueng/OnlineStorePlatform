FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Inventory.API/Inventory.API.csproj", "src/Services/Inventory.API/"]
COPY ["src/Shared/Store.Shared.Common/Store.Shared.Common.csproj", "src/Shared/Store.Shared.Common/"]
COPY ["src/Shared/Store.Shared.Events/Store.Shared.Events.csproj", "src/Shared/Store.Shared.Events/"]
RUN dotnet restore "src/Services/Inventory.API/Inventory.API.csproj"
COPY . .
RUN dotnet publish "src/Services/Inventory.API/Inventory.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --retries=18 --start-period=40s CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Inventory.API.dll"]
