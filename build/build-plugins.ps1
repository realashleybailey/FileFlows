Write-Output "##################################"
Write-Output "###      Building Plugins      ###"
Write-Output "##################################"


$output2 = $args[0]
#if ([String]::IsNullOrEmpty($output)) {
$output = '../FileFlows/deploy/Plugins';
#}

$year = (Get-Date).year
$copyright = "Copyright $year - John Andrews"


# build plugin
# build 0.0.1.0 so included one is always greater
dotnet.exe build ..\Plugin\Plugin.csproj /p:WarningLevel=1 --configuration Release  /p:AssemblyVersion=0.0.1.0 /p:Version=0.0.1.0 /p:CopyRight=$copyright --output ../../FileFlowsPlugins

Remove-Item ../../FileFlowsPlugins/FileFlows.Plugin.deps.json -ErrorAction SilentlyContinue

Push-Location ..\..\FileFlowsPlugins

Remove-Item Builds  -Recurse -ErrorAction SilentlyContinue

$revision = (git rev-list --count --first-parent HEAD) -join "`n"

$json = "[`n"

Get-ChildItem -Path .\ -Filter *.csproj -Recurse -File -Name | ForEach-Object {
    # update version number of builds
    (Get-Content $_) `
        -replace '(?<=(Version>([\d]+\.){3}))([\d]+)(?=<)', $revision |
    Out-File $_

        
    $package = [System.IO.Path]::GetFileNameWithoutExtension($_) 
    Write-Output "Building Plugin $package"

    # build an instance for FileFlow local code
    dotnet build $_ /p:WarningLevel=1 --configuration Release /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary --output:$output/$package/  
    Remove-Item $output/$package/FileFlows.Plugin.dll -ErrorAction SilentlyContinue
    Remove-Item $output/$package/FileFlows.Plugin.pdb -ErrorAction SilentlyContinue
    Remove-Item $output/$package/*.deps.json -ErrorAction SilentlyContinue
    Remove-Item $output/$package/ref -Recurse -ErrorAction SilentlyContinue

    Push-Location ../FileFlows/PluginInfoGenerator
    dotnet run ../deploy/Plugins/$package/$package.dll ../../FileFlowsPlugins/$package/$package.csproj
    Pop-Location
    Move-Item $output/$package/*.plugininfo $output/$package/.plugininfo -Force
    Move-Item $output/$package/*.nfo $output/$package/.nfo -Force

    if ( (Test-Path -Path $output/$package/.plugininfo -PathType Leaf) -and (Test-Path -Path $output/$package/.nfo -PathType Leaf)) {

        # only actually create the plugin if plugins were found in it      
        
        #read nfo file
        $pluginNfo = [System.IO.File]::ReadAllText("$output/$package/.nfo");
        Write-Output "Plugin NFO: $pluginNfo"
        $json += $pluginNfo + ",`n"
        Remove-Item $output/$package/.nfo -Force

        Move-Item $output/$package/*.en.json $output/$package/en.json -Force

        # construct .ffplugin file
        $compress = @{
            Path             = "$output/$package/*"
            CompressionLevel = "Optimal"
            DestinationPath  = "$output/$package.zip"
        }
        Write-Output "Creating zip file $output/$package.zip"

        Compress-Archive @compress

        Write-Output "Creating plugin file $output/$package.ffplugin"
        Move-Item "$output/$package.zip" "$output/$package.ffplugin" -Force

        if ([String]::IsNullOrEmpty($output2) -eq $false) {
            Write-Output "Moving file to $output2"        
            Copy-Item "$output/$package.ffplugin" "$output2/" -Force
        }
    }
    else {
        Write-Error "WARNING: Failed to generate plugin info files for: $package"        
    }

    Remove-Item $output/$package -Recurse -ErrorAction SilentlyContinue
}

$json = $json.Substring(0, $json.lastIndexOf(',')) + "`n"
$json += ']';

Set-Content -Path "$output/plugins.json" -Value $json

Pop-Location
