. .\build-variables.ps1

Remove-Item ../deploy/* -Recurse -Force -Exclude *.ffplugin -ErrorAction SilentlyContinue 


$dev = $args[0] -eq '--dev'

if ($dev -eq $false) {
    if ([System.IO.Directory]::Exists("../deploy/Plugins") -eq $false) {
        Write-Error "ERROR: No plugins directory found"
        return
    }
}

.\build-spellcheck.ps1
.\build-flowrunner.ps1
.\build-server.ps1
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