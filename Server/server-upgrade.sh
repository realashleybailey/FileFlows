#!/usr/bin/env bash

if [ "$1" = "systemd" ]; then
  systemctl stop fileflows.service
elif [ "$1" != "docker" ]; then 
  kill %1
fi

cd ..
rm -rf Server
rm -rf FlowRunner
rm run-node.sh
rm run-server.sh

mv Update/FlowRunner FlowRunner
mv Update/Server Server
mv Update/run-node.sh run-node.sh
mv Update/run-server.sh run-server.sh

rm -rf Update

if [ "$1" = "systemd" ]; then
  systemctl start fileflows.service  
elif [ "$1" != "docker" ]; then 
  chmod +x run-server.sh
  ./run-server.sh
fi