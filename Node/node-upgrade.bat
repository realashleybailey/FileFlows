@echo off

IF %1=="UPDATE" GOTO RunUpdate
copy node-upgrade.bat ..\
start ..\node-upgrade.bat "UPDATE" %1 & exit
GOTO Done

:RunUpdate
cd ..
timeout /t 3
echo Stopping FileFlows Node if running
taskkill %2

echo.
echo Removing previous version
copy "fileflows.config" "NodeUpdate\fileflows.config"
FOR /d %%a IN ("*.*") DO IF /i NOT "%%~nxa"=="NodeUpdate" RD /S /Q "%%a"
FOR %%a IN ("*") DO IF /i NOT "%%~nxa"=="node-upgrade.bat" DEL "%%a"

echo.
echo Copying Node update files
move NodeUpdate\FileFlows-Runner FileFlows-Runner
move NodeUpdate\runtimes runtimes
move NodeUpdate\* .

echo.
echo Deleting NodeUpdate directory
rmdir /q /s NodeUpdate

echo.
echo Starting FileFlows Node
start dotnet FileFlows.Node.dll

if exist node-upgrade.bat goto Done
del node-upgrade.bat & exit

:Done
exit
