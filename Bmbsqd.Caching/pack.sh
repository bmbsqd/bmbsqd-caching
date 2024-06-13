#!/bin/sh
CONFIG=Release
rm bin/$CONFIG/Bmbsqd.Caching.*.nupkg
dotnet build --nologo true --configuration $CONFIG
dotnet pack --nologo true --include-source true --configuration $CONFIG
dotnet nuget push bin/$CONFIG/Bmbsqd.Caching.*.nupkg --source https://api.nuget.org/v3/index.json --api-key $NUGET_API_KEY --skip-duplicate