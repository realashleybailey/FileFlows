Write-Output "##################################"
Write-Output "###      Building Plugins      ###"
Write-Output "##################################"


$output = $args[0]
if ([String]::IsNullOrEmpty($output)) {
    $output = '../FileFlows/deploy/Plugins';
}

$year = (Get-Date).year
$copyright = "Copyright $year - John Andrews"


# build plugin
# build 0.0.1.0 so included one is always greater
dotnet.exe build ..\Plugin\Plugin.csproj --configuration Release  /p:AssemblyVersion=0.0.1.0 /p:Version=0.0.1.0 /p:CopyRight=$copyright --output ../../FileFlowsPlugins /nowarn:CS8618 /nowarn:CS8601 /nowarn:CS8602 /nowarn:CS8603 /nowarn:CS8604 /nowarn:CS8618 /nowarn:CS8625

Remove-Item ../../FileFlowsPlugins/FileFlows.Plugin.deps.json -ErrorAction SilentlyContinue

Push-Location ..\..\FileFlowsPlugins

Remove-Item Builds  -Recurse -ErrorAction SilentlyContinue

$revision = (git rev-list --count --first-parent HEAD) -join "`n"

Get-ChildItem -Path .\ -Filter *.csproj -Recurse -File -Name | ForEach-Object {
    # update version number of builds
    (Get-Content $_) `
        -replace '(?<=(Version>([\d]+\.){3}))([\d]+)(?=<)', $revision |
    Out-File $_

    $name = [System.IO.Path]::GetFileNameWithoutExtension($_) 
    $version = [Regex]::Match((Get-Content $_), "(?<=(Version>))([\d]+\.){3}[\d]+(?=<)").Value
    
    $json += "`t{`n"
    $json += "`t`t""Name"": ""$name"",`n"
    $json += "`t`t""Version"": ""$version"",`n"
    $json += "`t`t""Package"": ""https://github.com/revenz/FileFlowsPlugins/blob/master/Builds/" + $name + ".zip?raw=true""`n"
    $json += "`t},`n"

    # build an instance for FileFlow local code
    dotnet build $_ --configuration Release /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary --output:$output/$name/$version
    Remove-Item $output/$name/$version/FileFLows.Plugin.dll -ErrorAction SilentlyContinue
    Remove-Item $output/$name/$version/FileFLows.Plugin.pdb -ErrorAction SilentlyContinue
    Remove-Item $output/$name/$version/*.deps.json -ErrorAction SilentlyContinue
    Remove-Item $output/$name/$version/ref -Recurse -ErrorAction SilentlyContinue
}

Pop-Location
