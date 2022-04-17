$version = Get-Content .\version.txt -Raw 
Write-Host "##teamcity[setParameter name='env.FF_VERSION' value='$version']"