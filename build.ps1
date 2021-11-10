
if (Test-Path .\zpublish) {
    Remove-Item .\zpublish -Recurse -Force
}

dotnet.exe publish Server\Server.csproj --runtime linux-x64 --configuration Release --self-contained --output zpublish

dotnet.exe publish Client\Client.csproj --configuration Release --output zpublish


if (Test-Path .\wpublish) {
    Remove-Item .\wpublish -Recurse -Force
}

dotnet.exe publish Server\Server.csproj --runtime win-x64 --configuration Release --self-contained --output wpublish

dotnet.exe publish Client\Client.csproj --configuration Release --output wpublish