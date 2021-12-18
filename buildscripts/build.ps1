if (Test-Path ..\deploy) {
    Remove-Item ..\deploy -Recurse -Force
}

.\build-plugins.ps1
.\build-spellcheck.ps1
.\build-winnode.ps1
.\build-server.ps1
.\build-server.ps1 --linux

# no longer need plugins, delete them
Remove-Item ..\deploy\Plugins -Recurse -ErrorAction SilentlyContinue 