#!/usr/bin/env bash

if [ "$1" != "docker" ]; then 
  kill %1
fi

./run-server.sh