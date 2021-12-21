$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.1.0.$revision"
$year = (Get-Date).year
$copyright = "Copyright $year - John Andrews"

dotnet.exe publish ..\WindowsNode\WindowsNode.csproj --configuration Release --self-contained /p:AssemblyVersion=$version /p:Version=$version /p:CopyRight=$copyright /p:WarningLevel=0 --output ..\deploy\FileFlows-Node 

if ((Test-Path ..\deploy\plugins) -eq $true) {
    Write-Output "Copying plugins"
    Copy-Item -Path ..\deploy\Plugins -Filter "*.*" -Recurse -Destination ..\deploy\FileFlows-Node\Plugins -Container
}


(Get-Content installers\WindowsServerInstaller\Program.cs) -replace '([\d]+.){3}[\d]+', "$version" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii
(Get-Content installers\WindowsServerInstaller\Program.cs) -replace 'Node = false', "Node = true" | Out-File  installers\WindowsServerInstaller\Program.cs -Encoding ascii

if (Test-Path -Path 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' -PathType Leaf) {        
    $curDir = Get-Location
    & 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' installers\FileFlowInstallers.sln
}
else {
    msbuild.exe installers\FileFlowInstallers.sln
}


