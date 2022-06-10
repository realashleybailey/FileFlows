@echo off

IF %1=="START" GOTO StartFileFlows
start restart.bat "START" %1 & exit
GOTO Done

:StartFileFlows
timeout /t 3
echo Stopping FileFlows Server if running
taskkill /PID %2

echo.
echo Starting FileFlows Server
start dotnet FileFlows.Server.dll

:Done
exit
