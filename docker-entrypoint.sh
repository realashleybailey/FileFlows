#!/usr/bin/env bash

# set -e makes the script exit when a command fails.
set -e

if [[ "$FFNODE" == 'true' || "$FFNODE" == '1' || "$1" = '--node' ]]; then

    # check if there is an upgrade to apply
    if test -f "NodeUpdate/node-upgrade.sh"; then
        echo "Upgrade found"
        echo "exec /root/.dotnet/dotnet FileFlows.Node.dll --docker" > 'NodeUpdate/run-node.sh'
        chmod +x NodeUpdate/run-node.sh
        chmod +x NodeUpdate/node-upgrade.sh
        cd NodeUpdate
        exec node-upgrade.sh docker
    fi

    printf "Launching node\n"
    exec /root/.dotnet/dotnet FileFlows.Node.dll --docker
else
    printf "Launching server\n"
    exec /root/.dotnet/dotnet FileFlows.Server.dll --urls=http://*:5000 --docker
fi