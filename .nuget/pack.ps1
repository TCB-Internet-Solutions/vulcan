# custom nuspec pack
$path = '**\*.nuspec'
$nuget = '.\.nuget\nuget.exe'
$year = [System.DateTime]::Now.Year
$specFiles = Get-ChildItem $path -Recurse

foreach($file in $specFiles)
{
	$dll = $file.fullname.Replace($file.Name, "")
	$dll = $dll + "bin\release\" + $file.Name.Replace(".nuspec", ".dll")
	$versionFullString = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll).ProductVersion
	$version = $versionFullString.Split(" ")[0] 
	Write-Host "Found " $version " from " $dll
	& $nuget pack $file -properties "version=$version;year=$year" -symbols
}