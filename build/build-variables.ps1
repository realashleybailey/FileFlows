$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.4.1.$revision"
$year = (Get-Date).year
$copyright = "Copyright $year - John Andrews"