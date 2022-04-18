#!/usr/bin/env bash

# set -e makes the script exit when a command fails.
set -e

if [[ "$FFNODE" == 'true' || "$FFNODE" == '1' || "$1" = '--node' ]]; then
    printf "Launching node\n"
    exec /root/.dotnet/dotnet FileFlows.Node.dll --docker
else
    printf "Launching server\n"
    exec /root/.dotnet/dotnet FileFlows.Server.dll --urls=http://*:5000 --docker
fi