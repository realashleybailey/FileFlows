Write-Output "Building Spell Check"

# spell check the output
if (Test-Path -Path '../../spellcheck/spellcheck.csproj' -PathType Leaf) {
    Write-Output "Checking spelling"
    Push-Location ../../spellcheck
    & dotnet.exe run ../../fileflows
    Pop-Location
    if ($LASTEXITCODE -ne 0 ) {
        Write-Error "Spellcheck failed"
        exit
    }
}