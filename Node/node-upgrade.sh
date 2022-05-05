#!/usr/bin/env bash

kill %1

cp ../fileflows.config fileflows.config
rmdir -rf ../FileFlows-Runner
rmdir -rf ../Logs
rmdir -rf ../runtimes
rm ../*

mv * ../

cd ..
rm -rf NodeUpdate
chmod +x run.sh
./run.sh