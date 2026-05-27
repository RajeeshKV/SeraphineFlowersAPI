#!/usr/bin/env bash
set -euo pipefail

dotnet restore
dotnet ef database update \
  --project src/MonsoonMasala.Infrastructure/MonsoonMasala.Infrastructure.csproj \
  --startup-project src/MonsoonMasala.Api/MonsoonMasala.Api.csproj
