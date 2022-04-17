#!/usr/bin/env bash
if [ "$FFNODE" = 'true' ] || [ "$1" = '--node' ] then   
    printf "Launching node\n"
    exec /root/.dotnet/dotnet FileFlows.Node.dll
else then
    printf "Launching server\n"
    exec /root/.dotnet/dotnet FileFlows.Server.dll --urls=http://*:5000 --docker
fi