FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
ENV DOTNET_SYSTEM_NET_DISABLEIPV6=1

COPY src/backend/Directory.Build.props src/backend/Directory.Build.props
COPY src/backend/Directory.Packages.props src/backend/Directory.Packages.props
COPY src/backend/NmosController.sln src/backend/NmosController.sln
COPY src/backend/NmosController.Api/NmosController.Api.csproj src/backend/NmosController.Api/NmosController.Api.csproj
COPY src/backend/NmosController.Application/NmosController.Application.csproj src/backend/NmosController.Application/NmosController.Application.csproj
COPY src/backend/NmosController.Contracts/NmosController.Contracts.csproj src/backend/NmosController.Contracts/NmosController.Contracts.csproj
COPY src/backend/NmosController.Domain/NmosController.Domain.csproj src/backend/NmosController.Domain/NmosController.Domain.csproj
COPY src/backend/NmosController.Infrastructure/NmosController.Infrastructure.csproj src/backend/NmosController.Infrastructure/NmosController.Infrastructure.csproj

RUN set -eux; \
    restore_cmd="dotnet restore src/backend/NmosController.Api/NmosController.Api.csproj --source https://api.nuget.org/v3/index.json --disable-parallel"; \
    max_attempts=6; \
    attempt=1; \
    until timeout 120s sh -c "$restore_cmd"; do \
      if [ "$attempt" -ge "$max_attempts" ]; then \
        echo "dotnet restore failed after ${max_attempts} attempts"; \
        exit 1; \
      fi; \
      sleep_seconds=$((attempt * 5)); \
      echo "dotnet restore attempt ${attempt} failed; retrying in ${sleep_seconds}s..."; \
      sleep "$sleep_seconds"; \
      attempt=$((attempt + 1)); \
    done

COPY src/backend src/backend

RUN set -eux; \
    restore_cmd="dotnet restore src/backend/NmosController.Api/NmosController.Api.csproj --source https://api.nuget.org/v3/index.json --disable-parallel"; \
    max_attempts=6; \
    attempt=1; \
    until timeout 120s sh -c "$restore_cmd"; do \
      if [ "$attempt" -ge "$max_attempts" ]; then \
        echo "dotnet restore failed after ${max_attempts} attempts (post-copy)"; \
        exit 1; \
      fi; \
      sleep_seconds=$((attempt * 5)); \
      echo "dotnet restore post-copy attempt ${attempt} failed; retrying in ${sleep_seconds}s..."; \
      sleep "$sleep_seconds"; \
      attempt=$((attempt + 1)); \
    done

RUN dotnet publish src/backend/NmosController.Api/NmosController.Api.csproj \
    --no-restore \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "NmosController.Api.dll"]
