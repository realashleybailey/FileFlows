#!/usr/bin/env bash

if [ "$1" = '--node' ]; then
    printf "Launching node\n"
    exec dotnet /app/FileFlows.Node.dll
else then
    printf "Launching server\n"
    exec dotnet /app/FileFlows.Server.dll --urls=http://*:5000 --docker
fi