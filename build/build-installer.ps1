$builddir = $args[0]
$programfile = "$builddir\installers\WindowsServerInstaller\Program.cs"

Write-Output "Building intaller with cscs.exe: $programfile"
& "$builddir\utils\wixsharp\cscs.exe" $programfile
