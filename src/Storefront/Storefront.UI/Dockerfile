FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Storefront/Storefront.UI/Storefront.UI.csproj", "src/Storefront/Storefront.UI/"]
RUN dotnet restore "src/Storefront/Storefront.UI/Storefront.UI.csproj"
COPY . .
RUN dotnet publish "src/Storefront/Storefront.UI/Storefront.UI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --retries=12 --start-period=20s CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Storefront.UI.dll"]
