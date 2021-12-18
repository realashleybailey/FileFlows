$linux = $args[0] -eq '--linux'
$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.0.1.$revision"
$copyright = "Copyright 2021 - John Andrews"

$csVersion = "string Version = ""$version"""

$runtime = 'win-x64'
$outdir = 'deploy/FileFlows-Windows'
$csproj = 'WindowsServer\WindowsServer.csproj'
if ($linux -eq $true) {
    $outdir = 'deploy/FileFlows-Linux'
    $runtime = 'linux-x64'    
    $csproj = 'Server\Server.csproj'
}
else {    
    if([System.IO.File]::Exists('..\Server\Program.cs')){
        Rename-Item ..\Server\Program.cs Program.cs.skip 
    }
}

Push-Location ..\

(Get-Content Client\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Client\Globals.cs
(Get-Content Server\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Server\Globals.cs

(Get-Content Server\Globals.cs) -replace 'public static bool Demo { get; set; } = (true|false);', "public static bool Demo { get; set; } = false;" | Out-File Server\Globals.cs

(Get-Content Server\Server.csproj) -replace '<RuntimeIdentifier>[^<]+<RuntimeIdentifier>', "<RuntimeIdentifier>$runtime</RuntimeIdentifier>" | Out-File Server\Server.csproj
if( $linux -eq $true){
    (Get-Content Server\Server.csproj) -replace '<OutputType>[^<]+<OutputType>', "" | Out-File Server\Server.csproj
}


dotnet.exe publish $csproj --runtime $runtime --configuration Release --self-contained --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright
dotnet.exe publish Client\Client.csproj --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright

(Get-Content $outdir\wwwroot\index.html) -replace ' && location.hostname !== ''localhost''', '' | Out-File $outdir\wwwroot\index.html -encoding ascii

Remove-Item $outdir\Plugins -Recurse -ErrorAction SilentlyContinue 
if ([System.IO.Directory]::Exists('deploy\Plugins')) {
    Copy-Item -Path deploy\Plugins -Filter "*.*" -Recurse -Destination $outdir\Plugins -Container
}

$zip = "$outdir-$version.zip"

if([System.IO.File]::Exists($zip)){
    Remove-Item $zip
}

#Compress-Archive -Path "$outdir\*" $zip
#Remove-Item $outdir -Recurse -ErrorAction SilentlyContinue 

if ($linux -eq $false) {
    if([System.IO.File]::Exists('..\Server\Program.cs.skip')){
        Rename-Item Server\Program.cs.skip Program.cs 
    }
}
Pop-Location