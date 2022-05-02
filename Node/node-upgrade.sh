#!/usr/bin/env bash

kill %1

rmdir -rf ../FileFlows-Runner
rmdir -rf ../Logs
rmdir -rf ../runtimes
rm ../*

mv * ../

cd ..
rm -rf NodeUpdate
chmod +x run.sh
./run.sh