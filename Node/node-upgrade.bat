IF %1=="UPDATE" GOTO RunUpdate
start node-upgrade.bat "UPDATE" %1
GOTO Done

:RunUpdate
timeout /t 3
taskkill %2

del ../*.*
rmdir /s ../runtimes
rmdir /s ../FileFlows-Runner

for %%a in ("*") do move /y "%%~fa" ..\
for /d %%a in ("*") do move /y "%%~fa" ..\

cd ..
rmdir /s NodeUpdate
start dotnet .\FileFlows.Node.dll

:Done