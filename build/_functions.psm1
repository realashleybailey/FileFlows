function Compress
{
    param (
        [string] $path,
        [string] $destination
    )
    
    if($IsWindows)
    {        
        $zip = "$destination.zip"        

        if ([System.IO.File]::Exists($zip)) {
            Remove-Item $zip
        }

        $compress = @{
            Path             = "$path\*"
            CompressionLevel = "Optimal"
            DestinationPath  = $zip
        }
        Compress-Archive @compress
    }
    else
    {
        Write-Output "Creating tar file"
        $tar_file = "$destination.tar.gz"

        if ([System.IO.File]::Exists($tar_file)) {
            Remove-Item $tar_file
        }

        tar -cvzf "$tar_file" -C "$path" *
    }
}