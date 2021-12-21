$linux = $args[0] -eq '--linux'
$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.1.0.$revision"
$year = (Get-Date).year
$copyright = "Copyright $year - John Andrews"

$csVersion = "string Version = ""$version"""

$runtime = 'win-x64'
$outdir = 'deploy/FileFlows'
if ($linux -eq $true) {
    $outdir = 'deploy/FileFlows-Linux'
    $runtime = 'linux-x64'    
}

Push-Location ..\

(Get-Content Client\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Client\Globals.cs
(Get-Content Server\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Server\Globals.cs

(Get-Content Server\Globals.cs) -replace 'public static bool Demo { get; set; } = (true|false);', "public static bool Demo { get; set; } = false;" | Out-File Server\Globals.cs

if ( $linux -eq $true) {
    (Get-Content Server\Server.csproj) -replace '<AssemblyName>[^<]+</AssemblyName>', "<AssemblyName>FileFlows</AssemblyName>" | Out-File Server\Server.csproj
    dotnet.exe publish 'Server\Server.csproj' --runtime $runtime --configuration Release --self-contained --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright
}
else {    
    (Get-Content Server\Server.csproj) -replace '<AssemblyName>[^<]+</AssemblyName>', "<AssemblyName>FileFlows.Server</AssemblyName>" | Out-File Server\Server.csproj
    dotnet.exe publish 'WindowsServer\WindowsServer.csproj' --runtime $runtime --configuration Release --self-contained --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright    
    dotnet.exe publish 'Server\Server.csproj' --runtime $runtime --configuration Release --self-contained -p:PublishSingleFile=true --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright
}
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

if ( $linux -eq $false) {
    
    (Get-Content build\installers\WindowsServerInstaller\Program.cs) -replace '([\d]+.){3}[\d]+', "$version" | Out-File  build\installers\WindowsServerInstaller\Program.cs -Encoding ascii

    if (Test-Path -Path 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' -PathType Leaf) {        
        $curDir = Get-Location
        & 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' build\installers\FileFlowInstallers.sln        
    }
    else {
        msbuild.exe build\installers\FileFlowInstallers.sln
    }
}


if ($linux -eq $true) {
    $zip = "$outdir-$version.zip"

    if ([System.IO.File]::Exists($zip)) {
        Remove-Item $zip
    }

    Compress-Archive -Path "$outdir\*" $zip
    Remove-Item $outdir -Recurse -ErrorAction SilentlyContinue 
}

Pop-Location