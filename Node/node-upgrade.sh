#!/usr/bin/env bash

if [ "$1" != "docker" ]; then 
print 'dfsdf'
  kill %1
fi

cd ..
rm -rf Node
rm -rf FlowRunnner
del run-node.sh

mv NodeUpdate/FlowRunnner FlowRunnner
mv NodeUpdate/Node Node
mv run-node.sh run-node.sh

rm -rf NodeUpdate

if [ "$1" != "docker" ]; then 
  chmod +x run-node.sh
  ./run-node.sh
fi