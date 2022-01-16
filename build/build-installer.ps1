$sln = $args[0]
$dir = $sln.Substring(0, $sln.lastIndexOf('\'))

if (Test-Path -Path 'C:\Utils\cs-script\cscs.exe' -PathType Leaf) {    
    Write-Output "Building intaller with cscs.exe: $dir\Program.cs"
    & 'C:\Utils\cs-script\cscs.exe' "$dir\Program.cs"
}
elseif (Test-Path -Path 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' -PathType Leaf) {        
    & 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\msbuild.exe' $sln      
}
else {
    msbuild.exe $sln -m
}