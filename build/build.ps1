Remove-Item ../deploy/* -Recurse -Force -Exclude *.ffplugin -ErrorAction SilentlyContinue 


$dev = $args[0] -eq '--dev'

if ($dev -eq $false) {
    if ([System.IO.Directory]::Exists("../deploy/Plugins") -eq $false) {        
        Write-Error "ERROR: No plugins directory found"
        return
    }
}
. .\_variables.ps1

.\build-spellcheck.ps1
.\build-flowrunner.ps1
.\build-server.ps1
.\build-node.ps1

echo $version.Trim() > ../deploy/version.txt
echo $versionThree.Trim() > ../deploy/version3.txt

# no longer need plugins or flowrunner, delete them
Remove-Item ..\deploy\Plugins -Recurse -ErrorAction SilentlyContinue 