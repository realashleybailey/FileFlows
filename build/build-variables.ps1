$revision = (git rev-list --count --first-parent HEAD) -join "`n"
$version = "0.1.1.$revision"
$year = (Get-Date).year
$copyright = "Copyright $year - John Andrews"