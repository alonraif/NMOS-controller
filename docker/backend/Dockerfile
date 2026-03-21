FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/backend/Directory.Build.props src/backend/Directory.Build.props
COPY src/backend/Directory.Packages.props src/backend/Directory.Packages.props
COPY src/backend/NmosController.sln src/backend/NmosController.sln
COPY src/backend/NmosController.Api/NmosController.Api.csproj src/backend/NmosController.Api/NmosController.Api.csproj
COPY src/backend/NmosController.Application/NmosController.Application.csproj src/backend/NmosController.Application/NmosController.Application.csproj
COPY src/backend/NmosController.Contracts/NmosController.Contracts.csproj src/backend/NmosController.Contracts/NmosController.Contracts.csproj
COPY src/backend/NmosController.Domain/NmosController.Domain.csproj src/backend/NmosController.Domain/NmosController.Domain.csproj
COPY src/backend/NmosController.Infrastructure/NmosController.Infrastructure.csproj src/backend/NmosController.Infrastructure/NmosController.Infrastructure.csproj

RUN dotnet restore src/backend/NmosController.Api/NmosController.Api.csproj

COPY src/backend src/backend

RUN dotnet publish src/backend/NmosController.Api/NmosController.Api.csproj \
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
