taskkill %1

del ../*.*
rmdir /s ../runtimes
rmdir /s ../FileFlows-Runner

for %%a in ("*") do move /y "%%~fa" ..\
for /d %%a in ("*") do move /y "%%~fa" ..\

cd ..
start dotnet .\FileFlows.Node.dll