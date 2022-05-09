#!/usr/bin/env bash

(cd Server && nohup dotnet FileFlows.Server.dll >/dev/null 2>&1 & )