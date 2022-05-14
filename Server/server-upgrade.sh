#!/usr/bin/env bash

if [ "$1" != "docker" ]; then 
  kill %1
fi

cd ..
rm -rf Server
rm -rf Node
rm -rf FlowRunner
rm run-node.sh
rm run-server.sh

mv Update/FlowRunner FlowRunner
mv Update/Node Node
mv Update/Server Server
mv Update/run-node.sh run-node.sh
mv Update/run-server.sh run-server.sh

rm -rf Update

if [ "$1" != "docker" ]; then 
  chmod +x run-server.sh
  ./run-server.sh
fi