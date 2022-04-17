$version = Get-Content .\version3.txt -Raw 
$version = $version.Trim()
Write-Host "##teamcity[setParameter name='env.FF_VERSION' value='$version']"