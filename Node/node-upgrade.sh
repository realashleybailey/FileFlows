#!/usr/bin/env bash

if [ "$1" = "systemd" ]; then
  systemctl stop fileflows-node.service
elif [ "$1" != "docker" ]; then 
  kill %1
fi

cd ..
rm -rf Node
rm -rf FlowRunner
rm run-node.sh

mv NodeUpdate/FlowRunner FlowRunner
mv NodeUpdate/Node Node
mv NodeUpdate/run-node.sh run-node.sh

rm -rf NodeUpdate

if [ "$1" = "systemd" ]; then
  systemctl start fileflows-node.service  
elif [ "$1" != "docker" ]; then 
  chmod +x run-node.sh
  ./run-node.sh
fi