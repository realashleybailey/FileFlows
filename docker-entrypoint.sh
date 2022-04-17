#!/usr/bin/env bash

if [ "$FFNODE" = 'true' ] | [ "$1" = '--node' ]; then   
    printf "Launching node"
    exec dotnet FileFlows.Node.dll --docker
else
    printf "Launching server"
    exec dotnet FileFlows.Server.dll --urls=http://*:5000 --docker
fi