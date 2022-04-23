$version = Get-Content .\version3.txt -Raw 
$version = $version.Trim()
Write-Output "Setting env.FF_VERSION to: $version"
Write-Host "##teamcity[setParameter name='env.FF_VERSION' value='$version']"