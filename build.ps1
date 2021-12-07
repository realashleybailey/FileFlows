$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.0.1.$revision"
$copyright = "Copyright 2021 - John Andrews"


# build plugin
# build 0.0.1.0 so included one is always greater
dotnet.exe build Plugin\Plugin.csproj --configuration Release  /p:AssemblyVersion=0.0.1.0 /p:Version=0.0.1.0 /p:CopyRight=$copyright --output ../FileFlowsPlugins 
Remove-Item ../FileFlowsPlugins/Plugin.deps.json
# remove old plugins directories
Remove-Item .\Server\Plugins\* -Recurse -ErrorAction SilentlyContinue 

Push-Location ..\FileFlowsPlugins
.\buildplugins.ps1
Pop-Location

# spell check the output
if (Test-Path -Path '../spellcheck/spellcheck.csproj' -PathType Leaf) {
    Write-Output "Checking spelling"
    Push-Location ../spellcheck
    & dotnet.exe run ../fileflows
    Pop-Location
    if ($LASTEXITCODE -ne 0 ) {
        Write-Error "Spellcheck failed"
        exit
    }
}


if (Test-Path .\zpublish) {
    Remove-Item .\zpublish -Recurse -Force
}

$csVersion = "string Version = ""$version"""

(Get-Content Client\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Client\Globals.cs

(Get-Content Server\Globals.cs) -replace 'public static bool Demo { get; set; } = (true|false);', "public static bool Demo { get; set; } = false;" | Out-File Server\Globals.cs

dotnet.exe publish Server\Server.csproj --runtime linux-x64 --configuration Release --self-contained --output zpublish /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright
dotnet.exe publish Client\Client.csproj --configuration Release --output zpublish /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright


if (Test-Path .\wpublish) {
    Remove-Item .\wpublish -Recurse -Force
}

dotnet.exe publish Server\Server.csproj --runtime win-x64 --configuration Release --self-contained --output wpublish /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright
dotnet.exe publish Client\Client.csproj --configuration Release --output wpublish /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright
Remove-Item .\wpublish\Plugins -Recurse -ErrorAction SilentlyContinue 



Remove-Item .\zpublish\Plugins -Recurse -ErrorAction SilentlyContinue 
Copy-Item -Path Server\Plugins -Filter "*.*" -Recurse -Destination zpublish -Container
Remove-Item .\wpublish\Plugins -Recurse -ErrorAction SilentlyContinue 
Copy-Item -Path Server\Plugins -Filter "*.*" -Recurse -Destination wpublish -Container

#(Get-Content zpublish\wwwroot\index.html) -replace '#VERSION#', $version | Out-File zpublish\wwwroot\index.html
#(Get-Content wpublish\wwwroot\index.html) -replace '#VERSION#', $version | Out-File wpublish\wwwroot\index.html
