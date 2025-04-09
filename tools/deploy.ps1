  
param (
    [string] $version
)

while (!$version) {
    $version = Read-Host "Please specify version!"
}

$srcDir = "..\src\CloudPhotoSync.Service"
$stgDir = "staging\$version"
$targetMachine = "$env:ASUS-Zenbook-Maizal"
$serviceName = "COR2TECT Cloud Photo Sync"

.\deploy-dotnet-service.ps1 `
    $srcDir `
    $stgDir `
    $targetMachine `
    $serviceName 