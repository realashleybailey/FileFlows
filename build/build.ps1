if (Test-Path ..\deploy) {
    Remove-Item ..\deploy -Recurse -Force
}
$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.1.0.$revision"

.\build-plugins.ps1
.\build-spellcheck.ps1
.\build-server.ps1
.\build-server.ps1 --linux
.\build-winnode.ps1

$compress = @{
    Path             = "..\deploy\FileFlows", "..\deploy\FileFlows-Node"
    CompressionLevel = "Optimal"
    DestinationPath  = "..\deploy\FileFlows-Portable-$version.zip"
}
Compress-Archive @compress

Remove-Item ..\deploy\FileFlows-Node -Recurse -ErrorAction SilentlyContinue 
Remove-Item ..\deploy\FileFlows -Recurse -ErrorAction SilentlyContinue 

# no longer need plugins, delete them
Remove-Item ..\deploy\Plugins -Recurse -ErrorAction SilentlyContinue 