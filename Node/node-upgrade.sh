#!/usr/bin/env bash

if [%1 -ne 'docker']; then 
  kill %1
fi

cp ../fileflows.config fileflows.config
rmdir -rf ../FileFlows-Runner
rmdir -rf ../Logs
rmdir -rf ../runtimes
rm ../*

mv * ../

cd ..
rm -rf NodeUpdate
if [%1 -ne 'docker']; then 
  chmod +x run-node.sh
  ./run-node.sh
fi