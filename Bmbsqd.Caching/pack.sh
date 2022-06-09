#!/bin/sh
CONFIG=Release
dotnet build --nologo true --configuration $CONFIG
dotnet pack --nologo true --include-symbols true --include-source true --configuration $CONFIG
