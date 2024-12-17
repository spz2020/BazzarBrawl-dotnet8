$versionFile = "version.txt"
$version = Get-Content $versionFile
$currentVersion = [decimal]::Parse($version)
$newVersion = $currentVersion + 0.1
Set-Content $versionFile $newVersion.ToString("0.0")
