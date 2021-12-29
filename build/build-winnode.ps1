Write-Output "#################################"
Write-Output "###   Building Windows Node   ###"
Write-Output "#################################"

. .\build-variables.ps1

dotnet.exe publish ..\WindowsNode\WindowsNode.csproj /p:WarningLevel=1 --configuration Release --self-contained /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright  /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625 --output ..\deploy\FileFlows-Node

(Get-Content installers\WindowsServerInstaller\Program.cs) -replace '([\d]+.){3}[\d]+', "$version" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii
(Get-Content installers\WindowsServerInstaller\Program.cs) -replace 'Node = false', "Node = true" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii

if (Test-Path -Path 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' -PathType Leaf) {        
    $curDir = Get-Location
    & 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' installers\FileFlowInstallers.sln
}
else {
    msbuild.exe installers\FileFlowInstallers.sln
}


