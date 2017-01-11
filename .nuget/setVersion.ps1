# Sets AppVeyor variables for custom assembly info patching
$path = '**\AssemblyInfo.*'
$build = $env:APPVEYOR_BUILD_NUMBER
$commit = $env:APPVEYOR_REPO_COMMIT
$year = [System.DateTime]::Now.Year
$infos = Get-ChildItem $path -Recurse

foreach($file in $infos)
{
    Write-Host "Patching assembly info file: " $file.fullname
    Write-Host "For {build} = $build and {commit} = $commit"
    $x = Get-Content $file.fullname
    $x.Replace("{build}",$build).Replace("{commit}", $commit).Replace("{year}",$year) | Set-Content $file.fullName
}

Write-Host $infos.Count