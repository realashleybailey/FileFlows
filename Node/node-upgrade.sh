#!/usr/bin/env bash

kill %1

rm ../*
rm -rf ../runtimes
rm -rf ../FileFlows-Runner

mv * ../

cd ..
rm -rf NodeUpdate
start dotnet .\FileFlows.Node.dll