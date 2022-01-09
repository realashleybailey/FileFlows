Write-Output "Building Spell Check"

& dotnet utils/spellcheck/spellcheck.dll ../Client/wwwroot/i18n
if ($LASTEXITCODE -ne 0 ) {
    Write-Error "Spellcheck failed"
    exit
}