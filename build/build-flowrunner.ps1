Write-Output "##################################"
Write-Output "###    Building Flow Runner    ###"
Write-Output "##################################"

. .\build-variables.ps1

$linux = $args[0] -eq '--linux'

$runtime = 'win-x64'
$outdir = '../deploy/FileFlows-Runner'
if ($linux -eq $true) {
    $outdir = '../deploy/FileFlows-Linux'
    $runtime = 'linux-x64'    
}

(Get-Content ..\FlowRunner\FlowRunner.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File ..\FlowRunner\FlowRunner.csproj
(Get-Content ..\FlowRunner\FlowRunner.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File ..\FlowRunner\FlowRunner.csproj
(Get-Content ..\FlowRunner\FlowRunner.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File ..\FlowRunner\FlowRunner.csproj

if ( $linux -eq $true) {
    dotnet.exe publish '..\FlowRunner\FlowRunner.csproj' /p:WarningLevel=1 --runtime $runtime --configuration Release --self-contained --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625
}
else {    
    dotnet.exe publish '..\FlowRunner\FlowRunner.csproj' /p:WarningLevel=1 --runtime $runtime --configuration Release --self-contained -p:PublishSingleFile=true --output $outdir /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625

    if ((Test-Path ../deploy/FileFlows) -eq $false) {
        New-Item -Path ../deploy/FileFlows -ItemType directory
    }
    if ((Test-Path ../deploy/FileFlows-Node) -eq $false) {
        New-Item -Path ../deploy/FileFlows-Node -ItemType directory
    }

    Copy-Item "$outdir/FileFlows.FlowRunner.exe" "../deploy/FileFlows-Node"
    Copy-Item "$outdir/FileFlows.FlowRunner.pdb" "../deploy/FileFlows-Node"
    Copy-Item "$outdir/FileFlows.FlowRunner.exe" "../deploy/FileFlows"
    Copy-Item "$outdir/FileFlows.FlowRunner.pdb" "../deploy/FileFlows"

    Remove-Item $outdir -Recurse -ErrorAction SilentlyContinue
}