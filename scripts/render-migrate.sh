#!/usr/bin/env bash
set -euo pipefail

dotnet restore
dotnet ef database update \
  --project src/SeraphineFlowers.Infrastructure/SeraphineFlowers.Infrastructure.csproj \
  --startup-project src/SeraphineFlowers.Api/SeraphineFlowers.Api.csproj
