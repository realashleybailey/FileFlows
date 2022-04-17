$version = Get-Content .\version.txt -Raw 
$version = $version.Trim()
Write-Host "##teamcity[setParameter name='env.FF_VERSION' value='$version']"