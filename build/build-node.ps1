Write-Output "#################################"
Write-Output "###   Building Windows Node   ###"
Write-Output "#################################"

. .\_variables.ps1
Import-Module .\_functions

$csVersion = "string Version = ""$version"""
Write-Output "Version: $version"

(Get-Content ..\Node\Globals.cs) -replace 'string Version = \"[\d\.]+\"', $csVersion | Out-File ..\Node\Globals.cs

(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<PublishSingleFile>[^<]+</PublishSingleFile>', "" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<RuntimeIdentifier>[^<]+</RuntimeIdentifier>', "" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<Version>[^<]+</Version>', "<Version>$version</Version>" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<ProductVersion>[^<]+</ProductVersion>', "<ProductVersion>$version</ProductVersion>" | Out-File ..\WindowsNode\WindowsNode.csproj
(Get-Content ..\WindowsNode\WindowsNode.csproj) -replace '<Copyright>[^<]+</Copyright>', "<Copyright>$copyright</Copyright>" | Out-File ..\WindowsNode\WindowsNode.csproj

& $dotnet_cmd publish ..\WindowsNode\WindowsNode.csproj /p:WarningLevel=1 --configuration Release /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright  /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625 --output ..\deploy\FileFlows-Node

if($IsWindows)
{
    (Get-Content installers\WindowsServerInstaller\Program.cs) -replace '([\d]+.){3}[\d]+', "$version" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii
    (Get-Content installers\WindowsServerInstaller\Program.cs) -replace 'Node = false', "Node = true" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii

    # build the installer
    .\build-installer.ps1 .
}

Compress "..\deploy\FileFlows-Node" "..\deploy\FileFlows-Node-$version"

Remove-Item ..\deploy\FileFlows-Node -Recurse -ErrorAction SilentlyContinue 



