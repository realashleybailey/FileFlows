@echo off

IF %1=="UPDATE" GOTO RunUpdate
copy server-upgrade.bat ..\
cd ..
start server-upgrade.bat "UPDATE" %1 & exit
GOTO Done

:RunUpdate
timeout /t 3
echo Stopping FileFlows Server if running
taskkill /PID %2

echo.
echo Removing previous version
rmdir /q /s Node
rmdir /q /s Server
rmdir /q /s FlowRunner

echo.
echo Copying Server update files
move Update/FlowRunner FlowRunner
move Update/Node Node
move Update/Server Server
rmdir /q /s Update

echo.
echo Starting FileFlows Server
cd Server
start dotnet FileFlows.Server.dll
cd .. 

if exist server-upgrade.bat goto Done
del server-upgrade.bat & exit

:Done
exit
