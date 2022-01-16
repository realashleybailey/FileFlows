Write-Output "#################################"
Write-Output "###      Building Server      ###"
Write-Output "#################################"

. .\build-variables.ps1

$outdir = 'deploy/FileFlows'
$csVersion = "string Version = ""$version"""
Push-Location ..\

(Get-Content Client\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Client\Globals.cs
(Get-Content Server\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Server\Globals.cs

(Get-Content Server\Server.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File Server\Server.csproj
(Get-Content Server\Server.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File Server\Server.csproj
(Get-Content Server\Server.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File Server\Server.csproj
(Get-Content Client\Client.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File Client\Client.csproj
(Get-Content Client\Client.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File Client\Client.csproj
(Get-Content Client\Client.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File Client\Client.csproj


(Get-Content Server\Globals.cs) -replace 'public static bool Demo { get; set; } = (true|false);', "public static bool Demo { get; set; } = false;" | Out-File Server\Globals.cs

(Get-Content Server\Server.csproj) -replace '<AssemblyName>[^<]+</AssemblyName>', "<AssemblyName>FileFlows.Server</AssemblyName>" | Out-File Server\Server.csproj
dotnet.exe publish 'WindowsServer\WindowsServer.csproj' /p:WarningLevel=1 --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625
dotnet.exe publish 'Server\Server.csproj' /p:WarningLevel=1 --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625

dotnet.exe publish Client\Client.csproj --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright

(Get-Content $outdir\wwwroot\index.html) -replace ' && location.hostname !== ''localhost''', '' | Out-File $outdir\wwwroot\index.html -encoding ascii
Remove-Item $outdir\wwwroot\_Framework\*.dll -Recurse -ErrorAction SilentlyContinue 
Remove-Item $outdir\wwwroot\_Framework\*.gz -Recurse -ErrorAction SilentlyContinue 
Remove-Item $outdir\wwwroot\*.scss -Recurse -ErrorAction SilentlyContinue 
Remove-Item $outdir\wwwroot\*.scss -Recurse -ErrorAction SilentlyContinue 

Remove-Item $outdir\Plugins -Recurse -ErrorAction SilentlyContinue 
if ((Test-Path deploy\plugins) -eq $true) {
    Write-Output "Copying plugins"
    Copy-Item -Path deploy\Plugins -Filter "*.*" -Recurse -Destination $outdir\Plugins -Container
}

    
(Get-Content build\installers\WindowsServerInstaller\Program.cs) -replace '([\d]+.){3}[\d]+', "$version" | Out-File  build\installers\WindowsServerInstaller\Program.cs -Encoding ascii
(Get-Content build\installers\WindowsServerInstaller\Program.cs) -replace 'Node = true', "Node = false" | Out-File  build\installers\WindowsServerInstaller\Program.cs -Encoding ascii

# build the installer
.\build-installer.ps1 build\installers\FileFlowInstallers.sln

$zip = "$outdir-$version.zip"

if ([System.IO.File]::Exists($zip)) {
    Remove-Item $zip
}

$compress = @{
    Path             = "$outdir\*"
    CompressionLevel = "Optimal"
    DestinationPath  = $zip
}
Compress-Archive @compress

Remove-Item $outdir -Recurse -ErrorAction SilentlyContinue 

Pop-Location