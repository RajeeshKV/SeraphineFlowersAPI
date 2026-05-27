# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SeraphineFlowers.sln ./
COPY src/SeraphineFlowers.Domain/SeraphineFlowers.Domain.csproj src/SeraphineFlowers.Domain/
COPY src/SeraphineFlowers.Application/SeraphineFlowers.Application.csproj src/SeraphineFlowers.Application/
COPY src/SeraphineFlowers.Infrastructure/SeraphineFlowers.Infrastructure.csproj src/SeraphineFlowers.Infrastructure/
COPY src/SeraphineFlowers.Api/SeraphineFlowers.Api.csproj src/SeraphineFlowers.Api/

RUN dotnet restore src/SeraphineFlowers.Api/SeraphineFlowers.Api.csproj

COPY . .

RUN dotnet publish src/SeraphineFlowers.Api/SeraphineFlowers.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="${PATH}:/root/.dotnet/tools"
ENV PORT=10000
ENV RUN_MIGRATIONS=true
EXPOSE 10000

RUN dotnet tool install --global dotnet-ef --version "8.*"

COPY --from=build /src /src
COPY --from=build /app/publish /app
COPY scripts/docker-entrypoint.sh /app/docker-entrypoint.sh

RUN chmod +x /app/docker-entrypoint.sh

ENTRYPOINT ["sh", "/app/docker-entrypoint.sh"]
