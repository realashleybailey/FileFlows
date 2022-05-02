taskkill %1

del ../*.*
rmdir /s ../runtimes
rmdir /s ../FileFlows-Runner

for %%a in ("*") do move /y "%%~fa" ..\
for /d %%a in ("*") do move /y "%%~fa" ..\

cd ..
rmdir /s NodeUpdate
start dotnet .\FileFlows.Node.dll