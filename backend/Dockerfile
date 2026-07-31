FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Domain/GestionProyectos.Domain/*.csproj src/Domain/GestionProyectos.Domain/
COPY src/Application/GestionProyectos.Application/*.csproj src/Application/GestionProyectos.Application/
COPY src/Infrastructure/GestionProyectos.Infrastructure/*.csproj src/Infrastructure/GestionProyectos.Infrastructure/
COPY src/API/GestionProyectos.Api/*.csproj src/API/GestionProyectos.Api/
RUN dotnet restore src/API/GestionProyectos.Api/GestionProyectos.Api.csproj

COPY src src
RUN dotnet publish src/API/GestionProyectos.Api/GestionProyectos.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GestionProyectos.Api.dll"]
