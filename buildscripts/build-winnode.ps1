$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.0.1.$revision"
$copyright = "Copyright 2021 - John Andrews"

dotnet.exe publish ..\WindowsNode\WindowsNode.csproj --configuration Release  /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /p:WarningLevel=0 --output ..\deploy\WindowsNode 

Copy-Item -Path ..\deploy\Plugins -Filter "*.*" -Recurse -Destination ..\deploy\WindowsNode\Plugins -Container

Compress-Archive -Path ..\deploy\WindowsNode\* ..\deploy\FileFlows-Windows-Node-$version.zip
Remove-Item ..\deploy\WindowsNode -Recurse -ErrorAction SilentlyContinue 

