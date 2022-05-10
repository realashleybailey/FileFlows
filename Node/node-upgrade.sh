#!/usr/bin/env bash

if [%1 -ne 'docker']; then 
  kill %1
fi

cd ..
rmdir -rf Node
rmdir -rf FlowRunnner
del run-node.sh

mv NodeUpdate/FlowRunnner ../FlowRunnner
mv NodeUpdate/Node ../Node
mv run-node.sh ../run-node.sh

rmdir -rf NodeUpdate

if [%1 -ne 'docker']; then 
  chmod +x run-node.sh
  ./run-node.sh
fi