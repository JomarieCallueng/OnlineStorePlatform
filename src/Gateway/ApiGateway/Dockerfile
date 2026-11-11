FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Gateway/ApiGateway/ApiGateway.csproj", "src/Gateway/ApiGateway/"]
RUN dotnet restore "src/Gateway/ApiGateway/ApiGateway.csproj"
COPY . .
RUN dotnet publish "src/Gateway/ApiGateway/ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --retries=18 --start-period=30s CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
