# syntax=docker/dockerfile:1
# Build context: solution root.
#   docker build -f src/Services/ServiceNameService/Dockerfile -t servicename-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/ServiceNameService/ src/Services/ServiceNameService/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore src/Services/ServiceNameService/ServiceName.API/ServiceName.API.csproj

#if (UseSqlite)
# Writable data directory for the SQLite file (the runtime image has no shell to create it).
RUN mkdir -p /data

#endif
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish src/Services/ServiceNameService/ServiceName.API/ServiceName.API.csproj \
      -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# Chiseled: distroless, non-root by default, no shell or package manager (minimal attack surface).
# "-extra" adds ICU and tzdata, required by database drivers for globalization.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app/publish .
#if (UseSqlite)
COPY --from=build --chown=$APP_UID:$APP_UID /data /data
#endif
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ServiceName.API.dll"]
