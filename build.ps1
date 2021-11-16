$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.0.1.$revision"
$copyright = "Copyright 2021 - John Andrews"

if (Test-Path .\zpublish) {
    Remove-Item .\zpublish -Recurse -Force
}

$csVersion = "string Version = ""$version"""

(Get-Content Client\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Client\Globals.cs

dotnet.exe build Plugins\BasicNodes\BasicNodes.csproj --configuration Release --output Server/Plugins /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright
dotnet.exe build Plugins\VideoNodes\VideoNodes.csproj --configuration Release --output Server/Plugins /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright

dotnet.exe publish Server\Server.csproj --runtime linux-x64 --configuration Release --self-contained --output zpublish /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright

dotnet.exe publish Client\Client.csproj --configuration Release --output zpublish /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright

Copy-Item -Path Server\Plugins\*.* -Destination zpublish\Plugins


if (Test-Path .\wpublish) {
    Remove-Item .\wpublish -Recurse -Force
}

dotnet.exe publish Server\Server.csproj --runtime win-x64 --configuration Release --self-contained --output wpublish /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright

dotnet.exe publish Client\Client.csproj --configuration Release --output wpublish /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright

Copy-Item -Path Server\Plugins\*.* -Destination wpublish\Plugins