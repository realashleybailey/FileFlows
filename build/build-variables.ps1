$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.3.3.$revision"
$year = (Get-Date).year
$copyright = "Copyright $year - John Andrews"