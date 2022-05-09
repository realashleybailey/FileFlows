#!/usr/bin/env bash

if [%1 -ne 'docker']; then 
  kill %1
fi

rmdir -rf Node
mv NodeUpdate node

del node-upgrade.sh

if [%1 -ne 'docker']; then 
  chmod +x run-node.sh
  ./run-node.sh
fi