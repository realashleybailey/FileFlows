Write-Output "#################################"
Write-Output "###       Building Demo       ###"
Write-Output "#################################"

. .\build-variables.ps1

$csVersion = "string Version = ""$version"""

$outdir = 'deploy/FileFlowsDemo'

Push-Location ..\

(Get-Content Client\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File Client\Globals.cs
(Get-Content Client\Client.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File Client\Client.csproj
(Get-Content Client\Client.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File Client\Client.csproj
(Get-Content Client\Client.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File Client\Client.csproj

dotnet.exe publish Client\Client.csproj --configuration Release --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /p=DefineConstants=DEMO

if ($LASTEXITCODE -ne 0 ) 
{
    Write-Error "Unexpected exit code from build"
}
else
{
    (Get-Content $outdir\wwwroot\index.html) -replace ' && location.hostname !== ''localhost''', '' | Out-File $outdir\wwwroot\index.html -encoding ascii
    Remove-Item $outdir\wwwroot\_Framework\*.dll -Recurse -ErrorAction SilentlyContinue 
    Remove-Item $outdir\wwwroot\_Framework\*.gz -Recurse -ErrorAction SilentlyContinue 
    Remove-Item $outdir\wwwroot\*.scss -Recurse -ErrorAction SilentlyContinue 
    Remove-Item $outdir\wwwroot\*.scss -Recurse -ErrorAction SilentlyContinue 

    Remove-Item $outdir\web.config -Recurse -ErrorAction SilentlyContinue 
    Copy-Item Build\demo.web.config $outdir\web.config

    $zip = "$outdir.zip"

    if ([System.IO.File]::Exists($zip)) {
        Remove-Item $zip
    }

    Compress-Archive -Path "$outdir\*" $zip
    Remove-Item $outdir -Recurse -ErrorAction SilentlyContinue 
}

Pop-Location