Write-Output "#################################"
Write-Output "###   Building Windows Node   ###"
Write-Output "#################################"

. .\build-variables.ps1

(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<PublishSingleFile>[^<]+</PublishSingleFile>', "" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<RuntimeIdentifier>[^<]+</RuntimeIdentifier>', "" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File ..\WindowsNode\WindowsNode.csproj

dotnet.exe publish ..\WindowsNode\WindowsNode.csproj /p:WarningLevel=1 --configuration Release /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright  /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625 --output ..\deploy\FileFlows-Node

(Get-Content installers\WindowsServerInstaller\Program.cs) -replace '([\d]+.){3}[\d]+', "$version" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii
(Get-Content installers\WindowsServerInstaller\Program.cs) -replace 'Node = false', "Node = true" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii

if (Test-Path -Path 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' -PathType Leaf) {        
    $curDir = Get-Location
    & 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' installers\FileFlowInstallers.sln
}
else {
    msbuild.exe installers\FileFlowInstallers.sln
}

$zip = ".\deploy\FileFlows-Node-$version.zip"

if ([System.IO.File]::Exists($zip)) {
    Remove-Item $zip
}

$compress = @{
    Path             = "..\deploy\FileFlows-Node\*"
    CompressionLevel = "Optimal"
    DestinationPath  = "..\deploy\FileFlows-Node-$version.zip"
}
Compress-Archive @compress

Remove-Item ..\deploy\FileFlows-Node -Recurse -ErrorAction SilentlyContinue 



