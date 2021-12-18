$linux = $args[0] -eq '--linux'
$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.0.1.$revision"
$copyright = "Copyright 2021 - John Andrews"

$csVersion = "string Version = ""$version"""

$runtime = 'win-x64'
$outdir = 'deploy/FileFlows-Windows'
if ($linux -eq $true) {
    $outdir = 'deploy/FileFlows-Linux'
    $runtime = 'linux-x64'
}

Push-Location ..\

(Get-Content Client\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Client\Globals.cs

(Get-Content Server\Globals.cs) -replace 'public static bool Demo { get; set; } = (true|false);', "public static bool Demo { get; set; } = false;" | Out-File Server\Globals.cs

(Get-Content Server\Server.csproj) -replace '<RuntimeIdentifier>[^<]+<RuntimeIdentifier>', "<RuntimeIdentifier>$runtime</RuntimeIdentifier>" | Out-File Server\Server.csproj

dotnet.exe publish Server\Server.csproj --runtime $runtime --configuration Release --self-contained --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright
dotnet.exe publish Client\Client.csproj --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright

Remove-Item $outdir\Plugins -Recurse -ErrorAction SilentlyContinue 
Copy-Item -Path deploy\Plugins -Filter "*.*" -Recurse -Destination $outdir\Plugins -Container

Compress-Archive -Path "$outdir\*" "$outdir-$version.zip"
Remove-Item $outdir -Recurse -ErrorAction SilentlyContinue 

Pop-Location