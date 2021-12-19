$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.0.1.$revision"
$copyright = "Copyright 2021 - John Andrews"
$year = (Get-Date).year

dotnet.exe publish ..\WindowsNode\WindowsNode.csproj --configuration Release --self-contained /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /p:WarningLevel=0 --output ..\deploy\FileFlows-Node 

if ((Test-Path ..\deploy\plugins) -eq $true) {
    Write-Output "Copying plugins"
    Copy-Item -Path ..\deploy\Plugins -Filter "*.*" -Recurse -Destination ..\deploy\FileFlows-Node\Plugins -Container
}

(Get-Content installer\fileflows-node.iss) -replace '0.0.0.0', "$version" | Out-File installer\fileflows-node-install.iss -Encoding ascii
$curDir = Get-Location
$curDir = [System.IO.Directory]::GetParent($curDir)
(Get-Content installer\fileflows-node-install.iss) -replace 'C\:\\Users\\john\\src\\FileFlows\\FileFlows\\', "$curDir\" | Out-File installer\fileflows-node-install.iss -Encoding ascii
(Get-Content installer\fileflows-node-install.iss) -replace '2020', "$year" | Out-File installer\fileflows-node-install.iss -Encoding ascii

& 'C:\Program Files (x86)\Inno Setup 6\iscc.exe' /O..\deploy 'installer\fileflows-node-install.iss' 


